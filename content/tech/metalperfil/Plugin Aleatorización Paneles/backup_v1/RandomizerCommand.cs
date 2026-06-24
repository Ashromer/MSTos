using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PluginAleatorizar.UI;

namespace PluginAleatorizar.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RandomizerCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var state = new RandomizerState();

            bool keepOpen = true;
            while (keepOpen)
            {
                var window = new RandomizerWindow(uiApp, state);
                window.ShowDialog();

                if (state.Action == WindowAction.PickWalls)
                {
                    try
                    {
                        var refs = uiApp.ActiveUIDocument.Selection.PickObjects(
                            ObjectType.Element,
                            new CurtainWallFilter(),
                            "Seleccione muros cortina y pulse Finalizar (Enter)");

                        state.SelectedWalls.Clear();
                        foreach (var r in refs)
                        {
                            if (uiApp.ActiveUIDocument.Document.GetElement(r) is Wall w && w.CurtainGrid != null)
                            {
                                state.SelectedWalls.Add(w);
                            }
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // Cancelado por el usuario; mantiene la selección previa
                    }
                    catch (Exception ex)
                    {
                        state.LogText += $"Error en selección: {ex.Message}\n";
                    }
                    // Reset action to None so it doesn't loop infinitely if reopened
                    state.Action = WindowAction.None; 
                }
                else
                {
                    // Closed by X, Cancel, or finished Randomizing
                    keepOpen = false;
                }
            }

            return Result.Succeeded;
        }
    }

    public enum WindowAction
    {
        None,
        PickWalls,
        Randomize
    }

    public class RandomizerState
    {
        public ObservableCollection<PanelTypeViewModel> TypeItems { get; } = new ObservableCollection<PanelTypeViewModel>();
        public List<Wall> SelectedWalls { get; } = new List<Wall>();
        public string LogText { get; set; } = "";
        public bool IsFixedSeed { get; set; } = false;
        public string SeedText { get; set; } = "42";
        public WindowAction Action { get; set; } = WindowAction.None;
    }

    internal class CurtainWallFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) =>
            elem is Wall w && w.CurtainGrid != null;

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
