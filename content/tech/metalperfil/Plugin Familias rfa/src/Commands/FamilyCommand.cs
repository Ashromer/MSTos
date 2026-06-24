using System;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using PluginFamilias.UI;

namespace PluginFamilias.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class FamilyCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;

            // Abrir la UI (ahora pasamos UIApplication para poder crear documentos nuevos)
            MainWindow window = new MainWindow(uiApp);
            new WindowInteropHelper(window).Owner = commandData.Application.MainWindowHandle;
            window.ShowDialog();
            
            return Result.Succeeded;
        }
    }
}
