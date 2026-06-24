using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginFachadaInteractiva.Commands;
using PluginFachadaInteractiva.Logic;

namespace PluginFachadaInteractiva.UI
{
    public partial class MainWindow : Window
    {
        private readonly UIApplication _uiApp;
        private readonly MainState _state;
        private readonly ExternalEvent _externalEvent;

        public ObservableCollection<MaterialItem> Materials { get; } = new ObservableCollection<MaterialItem>();

        private class WallDisplayItem
        {
            public string Display { get; set; } = "";
            public string Id { get; set; } = "";
        }

        public MainWindow(UIApplication uiApp, MainState state, ExternalEvent exEvent)
        {
            InitializeComponent();
            _uiApp = uiApp;
            _state = state;
            _externalEvent = exEvent;

            this.DataContext = _state;

            // Callbacks
            _state.LogCallback = (msg) => Dispatcher.Invoke(() => AppendLog(msg));
            _state.ProgressCallback = (st, val) => Dispatcher.Invoke(() => { txtStatus.Text = st; pbProgress.Value = val; });
            _state.NotifyUI = () => Dispatcher.Invoke(RefreshWallsList);

            LoadResources();
            LoadMaterials();
            LoadPanelTypes();
        }

        private void LoadResources()
        {
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
        }

        private void LoadMaterials()
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var mats = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .OrderBy(m => m.Name)
                .Select(m => new MaterialItem { Id = m.Id, Name = m.Name })
                .ToList();

            Materials.Clear();
            foreach (var m in mats) Materials.Add(m);
            cmbGlobalMaterial.ItemsSource = Materials;
        }

        private void LoadPanelTypes()
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var symbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_CurtainWallPanels)
                .Cast<FamilySymbol>()
                .OrderBy(s => s.Family.Name).ThenBy(s => s.Name)
                .ToList();

            _state.PanelTypes.Clear();
            foreach (var symbol in symbols)
            {
                double widthMm = FamilyWidthReader.GetWidthMm(doc, symbol);
                _state.PanelTypes.Add(new PanelTypeViewModel
                {
                    SymbolId = symbol.Id,
                    FamilyName = symbol.Family.Name,
                    TypeName = symbol.Name,
                    WidthMm = widthMm,
                    Weight = 0
                });
            }
            dgTypes.ItemsSource = _state.PanelTypes;
            AppendLog($"Cargados {symbols.Count} tipos de panel.");
        }

        private void RefreshWallsList()
        {
            lstWalls.ItemsSource = _state.SelectedWalls.Select(w => new WallDisplayItem
            {
                Display = $"{w.Name} (Largo: {((LocationCurve)w.Location).Curve.Length * 304.8:F0} mm)",
                Id = $"ID: {w.Id.Value}"
            }).ToList();

            txtWallCount.Text = $"{_state.SelectedWalls.Count} muros encontrados.";
        }

        // --- Eventos UI ---

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            _state.CurrentAction = CommandAction.ScanWalls;
            _externalEvent.Raise();
            AppendLog("Buscando muros con el parámetro 'MP_GenerarFachada'...");
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (_state.SelectedWalls.Count == 0)
            {
                MessageBox.Show("No hay muros seleccionados. Ejecute un escaneo primero.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_state.PanelTypes.Sum(t => t.Weight) <= 0)
            {
                MessageBox.Show("Asigne pesos (%) a los paneles para la distribución.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _state.CurrentAction = CommandAction.ApplyChanges;
            _externalEvent.Raise();
            AppendLog("Iniciando actualización de fachadas...");
        }

        private void chkGlobalMaterial_Changed(object sender, RoutedEventArgs e)
        {
            _state.UseGlobalMaterial = chkGlobalMaterial.IsChecked ?? false;
        }

        private void cmbGlobalMaterial_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbGlobalMaterial.SelectedItem is MaterialItem mi)
            {
                _state.GlobalMaterialId = mi.Id;
            }
        }

        private void AppendLog(string text)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
            txtLog.ScrollToEnd();
        }
    }
}
