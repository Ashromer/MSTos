using System;
using System.Reflection;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;
using System.IO;

namespace PluginFachadaInteractiva.Core
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "Metalperfil Tech";
            try { application.CreateRibbonTab(tabName); } catch { }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Fachadas");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            
            PushButtonData buttonData = new PushButtonData(
                "FachadaInteractiva",
                "Fachada\nInteractiva",
                assemblyPath,
                "PluginFachadaInteractiva.Commands.MainCommand");

            if (panel.AddItem(buttonData) is PushButton button)
            {
                button.ToolTip = "Generación y aleatorización interactiva de fachadas con control de materiales.";

                // Cargar icono si existe
                string? dir = Path.GetDirectoryName(assemblyPath);
                if (dir != null)
                {
                    string iconPath = Path.Combine(dir, "Resources", "logo32.png");
                    if (File.Exists(iconPath))
                    {
                        button.LargeImage = new BitmapImage(new Uri(iconPath));
                    }
                    string icon16Path = Path.Combine(dir, "Resources", "logo16.png");
                    if (File.Exists(icon16Path))
                    {
                        button.Image = new BitmapImage(new Uri(icon16Path));
                    }
                }
            }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
