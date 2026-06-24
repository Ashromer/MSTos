using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginAleatorizar.Commands;
using PluginAleatorizar.Logic;

namespace PluginAleatorizar.UI
{
    public partial class RandomizerWindow : Window
    {
        private readonly UIApplication _uiApp;
        private readonly RandomizerState _state;
        private readonly ExternalEvent _externalEvent;

        private class WallItem
        {
            public Wall Wall { get; init; } = null!;
            public string Display { get; init; } = "";
        }

        public RandomizerWindow(UIApplication uiApp, RandomizerState state, ExternalEvent exEvent)
        {
            InitializeComponent();
            _uiApp = uiApp;
            _state = state;
            _externalEvent = exEvent;

            this.DataContext = _state;

            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (dir != null)
            {
                string iconPath = Path.Combine(dir, "Resources", "logo32.png");
                if (File.Exists(iconPath))
                {
                    var img = new BitmapImage(new Uri(iconPath));
                    Icon = img;
                    imgLogo.Source = img;
                }
            }

            _state.NotifySelectionChanged = () => Dispatcher.Invoke(() =>
            {
                RefreshWallsList();
                AppendLog($"Seleccionados {_state.SelectedWalls.Count} muro(s).");
            });

            _state.LogCallback = (msg) => Dispatcher.Invoke(() => AppendLog(msg));

            _state.ProgressCallback = (status, val) => Dispatcher.Invoke(() =>
            {
                txtStatus.Text = status;
                pbProgress.Value = val;
            });

            LoadPanelTypes();
            RefreshWallsList();
        }

        // ── Tipos de panel ────────────────────────────────────────────────────

        private void LoadPanelTypes()
        {
            try
            {
                var doc = _uiApp.ActiveUIDocument.Document;

                var symbols = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_CurtainWallPanels)
                    .Cast<FamilySymbol>()
                    .OrderBy(s => s.Family.Name).ThenBy(s => s.Name)
                    .ToList();

                _state.TypeItems.Clear();
                foreach (var symbol in symbols)
                {
                    double widthMm = FamilyWidthReader.GetWidthMm(doc, symbol);

                    var vm = new PanelTypeViewModel
                    {
                        SymbolId = symbol.Id,
                        FamilyName = symbol.Family.Name,
                        TypeName = symbol.Name,
                        WidthMm = widthMm,
                        Weight = 0
                    };
                    vm.PropertyChanged += (_, _) => RefreshTotal();
                    _state.TypeItems.Add(vm);
                }

                AppendLog($"Cargados {_state.TypeItems.Count} tipos de panel.");
                RefreshTotal();
            }
            catch (Exception ex)
            {
                AppendLog($"Error al cargar tipos: {ex.Message}");
            }
        }

        private void btnReloadTypes_Click(object sender, RoutedEventArgs e) => LoadPanelTypes();

        private void btnDistribute_Click(object sender, RoutedEventArgs e)
        {
            var targets = dgTypes.SelectedItems.Cast<PanelTypeViewModel>().ToList();
            bool subset = targets.Count > 0;
            if (!subset) targets = _state.TypeItems.ToList();
            if (targets.Count == 0) return;

            if (subset)
                foreach (var vm in _state.TypeItems.Except(targets)) vm.Weight = 0;

            double each = Math.Round(100.0 / targets.Count, 1);
            double accumulated = 0;
            for (int i = 0; i < targets.Count - 1; i++)
            {
                targets[i].Weight = each;
                accumulated += each;
            }
            targets[^1].Weight = Math.Round(100.0 - accumulated, 1);

            RefreshTotal();
        }

        private static readonly Brush OkGreen = new SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 201, 176)); // #4EC9B0 (Green-blue)
        private static readonly Brush ErrorRed = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 115, 115)); // Soft red
        private static readonly Brush NeutralGray = new SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 153, 153)); // #999999

        private void RefreshTotal()
        {
            double total = _state.TypeItems.Sum(t => t.Weight);
            txtTotal.Text = $"Total: {total:F1} %";
            txtTotal.Foreground = Math.Abs(total - 100) < 0.5
                ? OkGreen
                : ErrorRed;
        }

        // ── Muros ─────────────────────────────────────────────────────────────

        private void btnPick_Click(object sender, RoutedEventArgs e)
        {
            _state.Action = WindowAction.PickWalls;
            _externalEvent.Raise();
        }

        private void btnRemoveWall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b && b.Tag is WallItem item)
            {
                _state.SelectedWalls.Remove(item.Wall);
                RefreshWallsList();
            }
        }

        private void RefreshWallsList()
        {
            lstWalls.ItemsSource = _state.SelectedWalls
                .Select(w => new WallItem { Wall = w, Display = WallDisplay(w) })
                .ToList();

            int count = _state.SelectedWalls.Count;
            txtWallCount.Text = count == 1 ? "1 muro" : $"{count} muros";
            txtWallCount.Foreground = count > 0 ? OkGreen : NeutralGray;
        }

        private static string WallDisplay(Wall w)
        {
            try
            {
                double lenMm = ((LocationCurve)w.Location).Curve.Length * 304.8;
                return $"Muro {w.Id.Value}  —  {lenMm:F0} mm  ·  {w.Name}";
            }
            catch
            {
                return $"Muro {w.Id.Value}";
            }
        }

        // ── Ejecutar ──────────────────────────────────────────────────────────

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (_state.SelectedWalls.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un muro cortina.");
                return;
            }

            var activeItems = _state.TypeItems.Where(x => x.Weight > 0).ToList();
            if (activeItems.Count == 0)
            {
                MessageBox.Show("Asigne un peso mayor a 0 a al menos un tipo de panel.");
                return;
            }

            if (activeItems.Any(x => x.WidthMm <= 0))
            {
                var noWidth = string.Join(", ", activeItems.Where(x => x.WidthMm <= 0).Select(x => x.TypeName));
                MessageBox.Show($"Estos tipos no tienen ancho válido: {noWidth}.\nQuíteles el peso o revise las familias.");
                return;
            }

            double totalWeight = activeItems.Sum(x => x.Weight);
            if (Math.Abs(totalWeight - 100) > 0.1)
            {
                var res = MessageBox.Show($"La suma de pesos es {totalWeight:F1}%. ¿Desea normalizar al 100%?",
                    "Pesos", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    foreach (var ti in activeItems) ti.Weight = Math.Round(ti.Weight / totalWeight * 100, 1);
                    RefreshTotal();
                }
                else return;
            }

            _state.Action = WindowAction.Randomize;
            _externalEvent.Raise();
            AppendLog("Iniciando proceso de aleatorización...");
        }

        private void AppendLog(string text)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
            txtLog.ScrollToEnd();
        }
    }
}
