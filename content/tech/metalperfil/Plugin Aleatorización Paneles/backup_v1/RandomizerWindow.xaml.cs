using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginAleatorizar.Logic;
using PluginAleatorizar.Commands;

namespace PluginAleatorizar.UI
{
    public partial class RandomizerWindow : Window
    {
        private readonly UIApplication _uiApp;
        private readonly RandomizerState _state;

        // Item simple para el ComboBox de familias
        private record FamilyItem(string Name, Family Family);

        public RandomizerWindow(UIApplication uiApp, RandomizerState state)
        {
            _uiApp = uiApp;
            _state = state;
            InitializeComponent();
            
            // Vincular datos desde el estado
            dgTypes.ItemsSource = _state.TypeItems;
            _state.TypeItems.CollectionChanged += (_, _) => RefreshTotal();
            
            txtLog.Text = _state.LogText;
            chkFixedSeed.IsChecked = _state.IsFixedSeed;
            txtSeed.Text = _state.SeedText;

            UpdateWallCountUI();
            RefreshTotal();
        }

        private void UpdateWallCountUI()
        {
            txtWallCount.Text = $"{_state.SelectedWalls.Count} muro(s) seleccionado(s)";
            txtWallCount.Foreground = _state.SelectedWalls.Count > 0
                ? Brushes.DarkGreen
                : Brushes.Gray;
        }

        // ── Cargar familias del proyecto ───────────────────────────────────────

        private void btnLoadFamilies_Click(object sender, RoutedEventArgs e)
        {
            var doc = _uiApp.ActiveUIDocument?.Document;
            if (doc == null) return;

            var families = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_CurtainWallPanels)
                .Cast<FamilySymbol>()
                .Select(fs => fs.Family)
                .GroupBy(f => f.Id)
                .Select(g => g.First())
                .OrderBy(f => f.Name)
                .Select(f => new FamilyItem(f.Name, f))
                .ToList();

            cmbFamilies.ItemsSource = families;

            if (families.Count == 0)
                Log("No se encontraron familias cargadas en el proyecto.");
            else
                Log($"Familias cargadas: {families.Count}. Seleccione una para añadir sus tipos.");
        }

        private void cmbFamilies_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbFamilies.SelectedItem is not FamilyItem selected) return;

            var doc = _uiApp.ActiveUIDocument?.Document;
            if (doc == null) return;

            // NO limpiamos la lista para permitir múltiples familias:
            // _state.TypeItems.Clear();

            var symbolIds = selected.Family.GetFamilySymbolIds();
            int addedCount = 0;
            foreach (var id in symbolIds)
            {
                if (doc.GetElement(id) is not FamilySymbol symbol) continue;

                // Evitar duplicados
                if (_state.TypeItems.Any(vm => vm.SymbolId == id)) continue;

                double widthMm = 0;
                try { widthMm = FamilyWidthReader.GetWidthMm(doc, symbol); }
                catch (Exception ex) { Log($"  [!] {symbol.Name}: no se pudo leer el ancho — {ex.Message}"); }

                var vm = new PanelTypeViewModel
                {
                    SymbolId = id,
                    FamilyName = selected.Name,
                    TypeName = symbol.Name,
                    WidthMm = widthMm,
                    Percentage = 0
                };
                vm.PropertyChanged += (_, _) => RefreshTotal();
                _state.TypeItems.Add(vm);
                addedCount++;
            }

            Log($"Familia '{selected.Name}': {addedCount} tipo(s) nuevo(s) añadido(s).");
            RefreshTotal();
        }

        // ── Distribuir porcentajes uniformemente ───────────────────────────────

        private void btnDistributeEqual_Click(object sender, RoutedEventArgs e)
        {
            if (_state.TypeItems.Count == 0) return;
            double each = Math.Round(100.0 / _state.TypeItems.Count, 1);
            double accumulated = 0;
            for (int i = 0; i < _state.TypeItems.Count - 1; i++)
            {
                _state.TypeItems[i].Percentage = each;
                accumulated += each;
            }
            // El último absorbe el resto para que sume exactamente 100
            _state.TypeItems[^1].Percentage = Math.Round(100.0 - accumulated, 1);
            RefreshTotal();
        }

        // ── Selección de muros en el modelo ───────────────────────────────────

        private void btnSelectWalls_Click(object sender, RoutedEventArgs e)
        {
            SaveState();
            _state.Action = WindowAction.PickWalls;
            this.Close(); // Cierra el diálogo para que el comando External retome y pida seleccionar sin romper el contexto
        }

        // ── Ejecutar aleatorización ────────────────────────────────────────────

        private void btnRandomize_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;
            SaveState();

            var doc = _uiApp.ActiveUIDocument.Document;

            int? seed = chkFixedSeed.IsChecked == true && int.TryParse(txtSeed.Text, out int s)
                ? s
                : null;

            var typeInfos = _state.TypeItems
                .Where(vm => vm.Percentage > 0 && vm.WidthMm > 0)
                .Select(vm => new PanelTypeInfo
                {
                    SymbolId = vm.SymbolId,
                    FamilyName = vm.FamilyName,
                    TypeName = vm.TypeName,
                    WidthMm = vm.WidthMm,
                    Percentage = vm.Percentage
                })
                .ToList();

            var generator = new PanelSequenceGenerator();
            var processor = new CurtainWallProcessor();

            Log($"\n── Inicio ─────────────────────────────────────────");
            Log($"Tipos activos: {typeInfos.Count}  |  Muros: {_state.SelectedWalls.Count}");
            Log($"Semilla: {(seed.HasValue ? seed.ToString() : "aleatoria")}");

            int wallSeed = seed ?? Environment.TickCount;

            foreach (var wall in _state.SelectedWalls)
            {
                var locCurve = (LocationCurve)wall.Location;
                double wallLengthMm = locCurve.Curve.Length * 304.8;
                Log($"\nMuro {wall.Id}  ({wallLengthMm:F0} mm)");

                var sequence = generator.Generate(typeInfos, wallLengthMm, out double remainder, wallSeed++);

                if (sequence.Count == 0)
                {
                    Log("  [!] No se pudo generar secuencia para este muro.");
                    continue;
                }

                LogSequenceSummary(sequence, wallLengthMm, remainder);

                try
                {
                    processor.Process(doc, wall, sequence, Log);
                }
                catch (Exception ex)
                {
                    Log($"  [ERROR] {ex.Message}");
                }
            }

            Log($"\n── Finalizado ─────────────────────────────────────");
            SaveState(); // Actualiza el log en el estado
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SaveState()
        {
            _state.IsFixedSeed = chkFixedSeed.IsChecked ?? false;
            _state.SeedText = txtSeed.Text;
            _state.LogText = txtLog.Text;
        }

        private bool ValidateInputs()
        {
            if (_state.TypeItems.Count == 0)
            {
                MessageBox.Show("Cargue una familia y seleccione sus tipos.", "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (_state.TypeItems.All(vm => vm.WidthMm <= 0))
            {
                MessageBox.Show("Ningún tipo tiene ancho válido. Compruebe que las familias están bien generadas.", "Sin ancho", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (_state.SelectedWalls.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un muro cortina.", "Sin muros", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            double total = _state.TypeItems.Sum(vm => vm.Percentage);
            if (Math.Abs(total - 100) > 1.0)
            {
                var res = MessageBox.Show(
                    $"Los porcentajes suman {total:F1}% (no es 100%). ¿Continuar de todos modos?",
                    "Porcentajes", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return false;
            }
            return true;
        }

        private void RefreshTotal()
        {
            double total = _state.TypeItems.Sum(vm => vm.Percentage);
            txtTotal.Text = $"{total:F1} %";
            txtTotal.Foreground = Math.Abs(total - 100) < 1.0
                ? Brushes.DarkGreen
                : Brushes.Crimson;
        }

        private void LogSequenceSummary(IList<PanelTypeInfo> sequence, double wallMm, double remainder)
        {
            var grouped = sequence
                .GroupBy(p => p.TypeName)
                .OrderByDescending(g => g.Count());

            var parts = grouped.Select(g => $"{g.Key}: {g.Count()} ({100.0 * g.Count() / sequence.Count:F0}%)");
            Log($"  Secuencia: {sequence.Count} paneles  → " + string.Join("  |  ", parts));
            if (Math.Abs(remainder) > 1)
                Log($"  Sobrante: {remainder:+0.#;-0.#} mm");
        }

        private void Log(string msg)
        {
            txtLog.Text += msg + Environment.NewLine;
            svLog.ScrollToBottom();
            FileLogger.Log(msg);
        }
    }
}
