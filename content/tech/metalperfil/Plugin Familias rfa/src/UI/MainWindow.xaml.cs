using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginFamilias.CAD;
using PluginFamilias.Core;

namespace PluginFamilias.UI
{
    public partial class MainWindow : Window
    {
        private UIApplication _uiApp;
        private List<PolylineData> _profiles = new List<PolylineData>();
        private PolylineData? _selectedProfile;
        private StreamWriter? _logWriter;

        public MainWindow(UIApplication uiApp)
        {
            InitializeComponent();
            _uiApp = uiApp;

            string defaultTemplate = @"C:\ProgramData\Autodesk\RVT 2026\Family Templates\English\Metric Curtain Wall Panel.rft";
            if (File.Exists(defaultTemplate)) txtTemplatePath.Text = defaultTemplate;

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

            UpdateCounter();
        }

        private void btnSelectTemplate_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Plantilla de Revit (*.rft)|*.rft";
            if (dlg.ShowDialog() == true) txtTemplatePath.Text = dlg.FileName;
        }

        private void btnSelectOutput_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    txtOutputPath.Text = dlg.SelectedPath;
            }
        }

        private void Log(string msg)
        {
            txtLog.Text = msg + "\n" + txtLog.Text;
            _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
            _logWriter?.Flush();
        }

        private void LogDetail(string msg)
        {
            _logWriter?.WriteLine(msg);
            _logWriter?.Flush();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Archivos de CAD (*.dxf;*.dwg)|*.dxf;*.dwg";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    CadExtractor extractor = new CadExtractor();
                    _profiles = extractor.Extract(openFileDialog.FileName);

                    lstProfiles.ItemsSource = null;
                    lstProfiles.ItemsSource = _profiles;

                    Log($"Cargados {_profiles.Count} perfiles de {Path.GetFileName(openFileDialog.FileName)}");
                    UpdateCounter();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error al cargar: " + ex.Message);
                }
            }
        }

        private void lstProfiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lstProfiles.SelectedItem is PolylineData selected)
            {
                _selectedProfile = selected;
                DrawPreview(selected);
            }
            else
            {
                _selectedProfile = null;
                canvasPreview.Children.Clear();
            }
        }

        private void canvasPreview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_selectedProfile != null)
            {
                DrawPreview(_selectedProfile);
            }
        }

        private void DrawPreview(PolylineData profile)
        {
            canvasPreview.Children.Clear();
            if (profile.Vertices == null || profile.Vertices.Count == 0) return;

            // 1. Obtener puntos de la silueta (normalizados)
            double minX = profile.Vertices.Min(v => v.X);
            double maxX = profile.Vertices.Max(v => v.X);
            double minY = profile.Vertices.Min(v => v.Y);
            double maxY = profile.Vertices.Max(v => v.Y);
            double cx = (minX + maxX) / 2.0;
            double cy = (minY + maxY) / 2.0;

            // Normalizar para dibujar centrado en el Canvas
            var norm = profile.Vertices
                .Select(v => new PolylineVertex { X = v.X - cx, Y = v.Y - cy, Bulge = v.Bulge })
                .ToList();

            // Usar la función de teselación para ver las curvas
            double tolMm = 0.5;
            var pts = FamilyGenerator.Tessellate(norm, tolMm);
            if (pts.Count < 2) return;

            // 2. Escalar para ajustar al Canvas
            double canvasW = canvasPreview.ActualWidth;
            double canvasH = canvasPreview.ActualHeight;
            if (canvasW <= 0) canvasW = 220; 
            if (canvasH <= 0) canvasH = 220;

            double pMinX = pts.Min(p => p.X);
            double pMaxX = pts.Max(p => p.X);
            double pMinY = pts.Min(p => p.Y);
            double pMaxY = pts.Max(p => p.Y);
            double pW = pMaxX - pMinX;
            double pH = pMaxY - pMinY;

            if (pW < 1e-6) pW = 1;
            if (pH < 1e-6) pH = 1;

            double margin = 15;
            double scale = Math.Min((canvasW - 2 * margin) / pW, (canvasH - 2 * margin) / pH);

            // 3. Crear y agregar las líneas del perfil
            var polyline = new System.Windows.Shapes.Polyline
            {
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 39, 44)), // Rojo Metalperfil
                StrokeThickness = 2,
                StrokeLineJoin = System.Windows.Media.PenLineJoin.Round
            };

            foreach (var pt in pts)
            {
                // Mapear coordenadas (invertir Y para sistema del Canvas de WPF)
                double x = margin + (pt.X - pMinX) * scale;
                double y = canvasH - (margin + (pt.Y - pMinY) * scale);
                polyline.Points.Add(new System.Windows.Point(x, y));
            }

            canvasPreview.Children.Add(polyline);
        }

        // ── Selección de perfiles ─────────────────────────────────────────────
        private void ProfileCheck_Click(object sender, RoutedEventArgs e) => UpdateCounter();

        private void btnSelectAll_Click(object sender, RoutedEventArgs e) => SetAllSelected(true);

        private void btnSelectNone_Click(object sender, RoutedEventArgs e) => SetAllSelected(false);

        private void SetAllSelected(bool value)
        {
            foreach (var p in _profiles) p.IsSelected = value;
            lstProfiles.ItemsSource = null;
            lstProfiles.ItemsSource = _profiles;
            UpdateCounter();
        }

        private void UpdateCounter()
        {
            int sel = _profiles.Count(p => p.IsSelected);
            txtCount.Text = $"{sel} / {_profiles.Count}";
            btnGenerate.IsEnabled = sel > 0;
        }

        // ── Generación ────────────────────────────────────────────────────────

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(txtTemplatePath.Text)) { System.Windows.MessageBox.Show("Seleccione una plantilla válida."); return; }
            if (!Directory.Exists(txtOutputPath.Text)) { System.Windows.MessageBox.Show("Seleccione una carpeta de salida."); return; }

            var selectedProfiles = _profiles.Where(p => p.IsSelected).ToList();
            if (selectedProfiles.Count == 0) { System.Windows.MessageBox.Show("No hay perfiles seleccionados."); return; }

            string logPath = BuildLogPath();

            btnGenerate.IsEnabled = false;
            btnLoad.IsEnabled = false;
            pbProgress.Visibility = System.Windows.Visibility.Visible;
            pbProgress.Maximum = selectedProfiles.Count;
            pbProgress.Value = 0;

            _uiApp.DialogBoxShowing += OnDialogBoxShowing;
            try
            {
                _logWriter = new StreamWriter(logPath, append: false, System.Text.Encoding.UTF8);
                _logWriter.WriteLine($"=== Sesión {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                _logWriter.WriteLine($"Plantilla : {txtTemplatePath.Text}");
                _logWriter.WriteLine($"Salida    : {txtOutputPath.Text}");
                _logWriter.WriteLine($"Perfiles  : {selectedProfiles.Count}");
                _logWriter.WriteLine();

                FamilyGenerator gen = new FamilyGenerator(_uiApp);
                int successCount = 0;
                int processed = 0;

                foreach (var profile in selectedProfiles)
                {
                    processed++;
                    txtStatus.Text = $"Generando {processed}/{selectedProfiles.Count} — {profile.Name}";
                    RefreshUi();

                    LogDetail($"--- {profile.Name} (vértices: {profile.Vertices.Count}) ---");
                    try
                    {
                        gen.GenerateBatch(profile, txtTemplatePath.Text, txtOutputPath.Text, LogDetail);
                        Log($"[OK] Generado: {profile.Name}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        string brief = ex.InnerException != null
                            ? $"{ex.Message} → {ex.InnerException.Message}"
                            : ex.Message;
                        Log($"[ERROR] {profile.Name}: {brief}");
                        LogDetail("  EXCEPCIÓN COMPLETA:");
                        WriteExceptionDetail(ex);
                    }

                    pbProgress.Value = processed;
                    RefreshUi();
                }

                _logWriter.WriteLine();
                _logWriter.WriteLine($"=== Resultado: {successCount}/{selectedProfiles.Count} OK  |  Fin {DateTime.Now:HH:mm:ss} ===");

                txtStatus.Text = $"Completado: {successCount}/{selectedProfiles.Count} familias generadas";

                System.Windows.MessageBox.Show(
                    $"Proceso finalizado. {successCount}/{selectedProfiles.Count} familias creadas.\n\nLog: {logPath}");
            }
            finally
            {
                _uiApp.DialogBoxShowing -= OnDialogBoxShowing;
                _logWriter?.Close();
                _logWriter = null;
                btnLoad.IsEnabled = true;
                UpdateCounter();
            }
        }

        // Fuerza el repintado de la UI entre familias (el bucle corre en el hilo de UI)
        private void RefreshUi() =>
            Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

        // Acepta automáticamente los diálogos de Revit durante el batch
        // (p. ej. el aviso de rendimiento por bocetos con muchos segmentos)
        private void OnDialogBoxShowing(object? sender, Autodesk.Revit.UI.Events.DialogBoxShowingEventArgs e)
        {
            LogDetail($"  [diálogo suprimido: {e.DialogId}]");
            e.OverrideResult(1);
        }

        private static string BuildLogPath()
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (dir == null) dir = Path.GetTempPath();
            string logsDir = Path.Combine(dir, "Logs");
            Directory.CreateDirectory(logsDir);
            return Path.Combine(logsDir, $"FamilyGen_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
        }

        private void WriteExceptionDetail(Exception ex, int depth = 0)
        {
            string pad = new string(' ', depth * 2 + 2);
            _logWriter?.WriteLine($"{pad}Tipo    : {ex.GetType().FullName}");
            _logWriter?.WriteLine($"{pad}Mensaje : {ex.Message}");
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                _logWriter?.WriteLine($"{pad}Stack   :");
                foreach (var line in ex.StackTrace.Split('\n'))
                    _logWriter?.WriteLine($"{pad}  {line.TrimEnd()}");
            }
            if (ex.InnerException != null)
            {
                _logWriter?.WriteLine($"{pad}--- InnerException ---");
                WriteExceptionDetail(ex.InnerException, depth + 1);
            }
            _logWriter?.Flush();
        }
    }
}
