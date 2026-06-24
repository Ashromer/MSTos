using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PluginAleatorizar.UI;
using PluginAleatorizar.Logic;

namespace PluginAleatorizar.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RandomizerCommand : IExternalCommand
    {
        private static RandomizerWindow _window;
        private static ExternalEvent _externalEvent;
        private static RandomizerHandler _handler;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (_window != null && _window.IsVisible)
            {
                _window.Focus();
                return Result.Succeeded;
            }

            var uiApp = commandData.Application;
            var state = new RandomizerState();
            
            _handler = new RandomizerHandler(state);
            _externalEvent = ExternalEvent.Create(_handler);
            
            _window = new RandomizerWindow(uiApp, state, _externalEvent);
            _window.Show();

            return Result.Succeeded;
        }
    }

    public class RandomizerHandler : IExternalEventHandler
    {
        private readonly RandomizerState _state;

        public RandomizerHandler(RandomizerState state) => _state = state;

        public void Execute(UIApplication app)
        {
            if (_state.Action == WindowAction.PickWalls)
            {
                try
                {
                    var refs = app.ActiveUIDocument.Selection.PickObjects(
                        ObjectType.Element,
                        new CurtainWallFilter(),
                        "Seleccione muros cortina y pulse Finalizar (Enter)");

                    _state.SelectedWalls.Clear();
                    foreach (var r in refs)
                    {
                        if (app.ActiveUIDocument.Document.GetElement(r) is Wall w && w.CurtainGrid != null)
                            _state.SelectedWalls.Add(w);
                    }
                    _state.NotifySelectionChanged?.Invoke();
                }
                catch { }
            }
            else if (_state.Action == WindowAction.Randomize)
            {
                RunRandomization(app.ActiveUIDocument.Document);
            }
            
            _state.Action = WindowAction.None;
        }

        private void RunRandomization(Document doc)
        {
            if (_state.SelectedWalls.Count == 0) return;

            var generator = new PanelSequenceGenerator();
            var processor = new CurtainWallProcessor();
            var sequences = new Dictionary<long, List<PanelTypeInfo>>();
            
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
                _state.SeedText = seedValue.ToString(); // Esto actualiza la UI
            }
            Random rnd = new Random(seedValue);

            // Leer solape de la UI
            double solapeMm = 40.0;
            if (double.TryParse(_state.SolapeText, out double solVal))
            {
                solapeMm = solVal;
            }

            var activeTypes = _state.TypeItems
                .Where(ti => ti.Weight > 0)
                .Select(ti => ti.ToInfo())
                .ToList();

            foreach (var wall in _state.SelectedWalls)
            {
                double lenMm = ((LocationCurve)wall.Location).Curve.Length * 304.8;
                double remainder;
                sequences[wall.Id.Value] = generator.Generate(
                    activeTypes,
                    lenMm, out remainder, rnd.Next(), solapeMm);
            }

            using (Transaction t = new Transaction(doc, "Metalperfil: Aleatorizar Fachada"))
            {
                t.Start();
                
                foreach (var ti in _state.TypeItems)
                {
                    if (ti.Weight > 0 && doc.GetElement(ti.SymbolId) is FamilySymbol fs && !fs.IsActive) fs.Activate();
                }

                int count = 0;
                foreach (var wall in _state.SelectedWalls)
                {
                    count++;
                    string wallName = $"Muro {count}/{_state.SelectedWalls.Count}";
                    
                    if (sequences.TryGetValue(wall.Id.Value, out var seq))
                    {
                        // Cada muro en su propia subtransacción: si uno falla
                        // (p. ej. rejillas que no se dejan borrar), se deshace
                        // solo ese muro y los demás se procesan limpios.
                        using (SubTransaction st = new SubTransaction(doc))
                        {
                            st.Start();
                            try
                            {
                                processor.Process(doc, wall, seq, solapeMm,
                                    msg => _state.LogCallback?.Invoke(msg),
                                    prog => _state.ProgressCallback?.Invoke(wallName, prog));
                                st.Commit();
                            }
                            catch (Exception ex)
                            {
                                st.RollBack();
                                _state.LogCallback?.Invoke(
                                    $"  [X] {wallName} (id {wall.Id}): error, muro revertido — {ex.Message}");
                            }
                        }
                    }
                }

                t.Commit();
            }
            _state.ProgressCallback?.Invoke("Proceso completado", 100);
        }

        public string GetName() => "Randomizer Handler";
    }

    public enum WindowAction { None, PickWalls, Randomize }

    public class RandomizerState : INotifyPropertyChanged
    {
        public ObservableCollection<PanelTypeViewModel> TypeItems { get; } = new ObservableCollection<PanelTypeViewModel>();
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

        public WindowAction Action { get; set; } = WindowAction.None;
        
        public Action NotifySelectionChanged { get; set; }
        public Action<string> LogCallback { get; set; }
        public Action<string, double> ProgressCallback { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    internal class CurtainWallFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is Wall w && w.CurtainGrid != null;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
