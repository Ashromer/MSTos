using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace PluginFamilias.Core
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // 1. Crear la Pestaña (Tab) personalizada
            string tabName = "Metalperfil Tech";
            try { application.CreateRibbonTab(tabName); } catch { }

            // 2. Crear el Panel
            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Automatización");

            // 3. Crear el Botón
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData btnData = new PushButtonData(
                "btnGenerador",
                "Generador\nFamilias",
                assemblyPath,
                "PluginFamilias.Commands.FamilyCommand"
            );

            if (panel.AddItem(btnData) is not PushButton btn)
                return Result.Succeeded;

            btn.ToolTip = "Genera familias de chapas metálicas a partir de perfiles CAD (DXF/DWG).";

            // 4. Cargar Icono (Logo)
            string? dir = System.IO.Path.GetDirectoryName(assemblyPath);
            if (dir != null)
            {
                string iconPath = System.IO.Path.Combine(dir, "Resources", "logo32.png");
                if (System.IO.File.Exists(iconPath))
                {
                    btn.LargeImage = new BitmapImage(new Uri(iconPath));
                }
                string icon16Path = System.IO.Path.Combine(dir, "Resources", "logo16.png");
                if (System.IO.File.Exists(icon16Path))
                {
                    btn.Image = new BitmapImage(new Uri(icon16Path));
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