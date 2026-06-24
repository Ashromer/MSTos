using System.Reflection;
using Autodesk.Revit.UI;

namespace PluginAleatorizar.Core
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            const string tabName = "Metalperfil Tech";
            try { application.CreateRibbonTab(tabName); } catch { }

            RibbonPanel? panel = null;
            try
            {
                panel = application.CreateRibbonPanel(tabName, "Diseño de Fachada");
            }
            catch
            {
                foreach (var p in application.GetRibbonPanels(tabName))
                {
                    if (p.Name == "Diseño de Fachada") { panel = p; break; }
                }
            }

            if (panel == null) return Result.Failed;

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            var btnData = new PushButtonData(
                "btnAleatorizar",
                "Aleatorizar\nPaneles",
                assemblyPath,
                "PluginAleatorizar.Commands.RandomizerCommand"
            );

            if (panel.AddItem(btnData) is PushButton btn)
            {
                btn.ToolTip = "Aleatoriza el reparto de tipos de panel en muros cortina, regenerando las rejillas según el ancho de cada tipo.";

                string? dir = System.IO.Path.GetDirectoryName(assemblyPath);
                if (dir != null)
                {
                    string icon32 = System.IO.Path.Combine(dir, "Resources", "logo32.png");
                    if (System.IO.File.Exists(icon32))
                        btn.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(icon32));
                    string icon16 = System.IO.Path.Combine(dir, "Resources", "logo16.png");
                    if (System.IO.File.Exists(icon16))
                        btn.Image = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(icon16));
                }
            }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
    }
}
