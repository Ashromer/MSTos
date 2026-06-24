using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PluginFachadaInteractiva.UI;
using PluginFachadaInteractiva.Logic;

namespace PluginFachadaInteractiva.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class MainCommand : IExternalCommand
    {
        private static MainWindow? _window;
        private static ExternalEvent? _externalEvent;
        private static MainHandler? _handler;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (_window != null && _window.IsVisible)
            {
                _window.Focus();
                return Result.Succeeded;
            }

            var uiApp = commandData.Application;
            var state = new MainState();
            
            _handler = new MainHandler(state);
            _externalEvent = ExternalEvent.Create(_handler);
            
            _window = new MainWindow(uiApp, state, _externalEvent);
            _window.Show();

            return Result.Succeeded;
        }
    }

    public class MainHandler : IExternalEventHandler
    {
        private readonly MainState _state;

        public MainHandler(MainState state) => _state = state;

        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;

            switch (_state.CurrentAction)
            {
                case CommandAction.ScanWalls:
                    ScanProjectWalls(doc);
                    break;
                case CommandAction.ApplyChanges:
                    RunProcess(doc);
                    break;
            }
            
            _state.CurrentAction = CommandAction.None;
        }

        private void ScanProjectWalls(Document doc)
        {
            var scanner = new WallScanner();
            var walls = scanner.GetTargetWalls(doc);
            
            _state.SelectedWalls.Clear();
            foreach (var w in walls) _state.SelectedWalls.Add(w);
            
            _state.LogCallback?.Invoke($"Escaneo completado: {walls.Count} muros encontrados con el parámetro 'MP_GenerarFachada'.");
            _state.NotifyUI?.Invoke();
        }

        private void RunProcess(Document doc)
        {
            if (_state.SelectedWalls.Count == 0)
            {
                _state.LogCallback?.Invoke("No hay muros seleccionados para procesar.");
                return;
            }

            var processor = new FachadaProcessor();
            
            // Lógica de semilla interactiva iterable
            int seedValue;
            if (_state.IsFixedSeed)
            {
                if (!int.TryParse(_state.SeedText, out seedValue))
                {
                    seedValue = 42;
                }
            }
            else
            {
                seedValue = new Random().Next(1, 999999);
                _state.SeedText = seedValue.ToString(); // Actualiza la UI
            }
            
            using (Transaction t = new Transaction(doc, "Metalperfil: Actualizar Fachada Interactiva"))
            {
                t.Start();
                
                // Asegurar que los tipos están activados
                foreach (var ti in _state.PanelTypes.Where(x => x.Weight > 0))
                {
                    if (doc.GetElement(ti.SymbolId) is FamilySymbol fs && !fs.IsActive) fs.Activate();
                }

                int count = 0;
                foreach (var wall in _state.SelectedWalls)
                {
                    count++;
                    string status = $"Muro {count}/{_state.SelectedWalls.Count}";
                    
                    try
                    {
                        processor.Process(doc, wall, _state, 
                            msg => _state.LogCallback?.Invoke(msg),
                            prog => _state.ProgressCallback?.Invoke(status, prog));
                    }
                    catch (Exception ex)
                    {
                        _state.LogCallback?.Invoke($"Error en muro {wall.Id}: {ex.Message}");
                    }
                }

                t.Commit();
            }
            _state.ProgressCallback?.Invoke("Proceso completado", 100);
        }

        public string GetName() => "Fachada Interactiva Handler";
    }

    public enum CommandAction { None, ScanWalls, ApplyChanges }

    public class MainState : INotifyPropertyChanged
    {
        public ObservableCollection<PanelTypeViewModel> PanelTypes { get; } = new ObservableCollection<PanelTypeViewModel>();
        public List<Wall> SelectedWalls { get; } = new List<Wall>();
        
        private bool _isFixedSeed = false;
        public bool IsFixedSeed
        {
            get => _isFixedSeed;
            set { _isFixedSeed = value; OnPropertyChanged(); }
        }

        private string _seedText = "42";
        public string SeedText
        {
            get => _seedText;
            set { _seedText = value; OnPropertyChanged(); }
        }

        private string _solapeText = "40";
        public string SolapeText
        {
            get => _solapeText;
            set { _solapeText = value; OnPropertyChanged(); }
        }
        
        public ElementId? GlobalMaterialId { get; set; }
        public bool UseGlobalMaterial { get; set; } = false;

        public CommandAction CurrentAction { get; set; } = CommandAction.None;
        
        public Action? NotifyUI { get; set; }
        public Action<string>? LogCallback { get; set; }
        public Action<string, double>? ProgressCallback { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
