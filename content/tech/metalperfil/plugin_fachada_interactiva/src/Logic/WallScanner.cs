using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace PluginFachadaInteractiva.Logic
{
    public class WallScanner
    {
        public const string ParameterName = "MP_GenerarFachada";

        public List<Wall> GetTargetWalls(Document doc)
        {
            EnsureParameterExists(doc);

            var walls = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .Where(w => IsTargetWall(w))
                .ToList();

            return walls;
        }

        private bool IsTargetWall(Wall wall)
        {
            // Primero, tiene que ser un muro cortina (o tener CurtainGrid)
            if (wall.CurtainGrid == null) return false;

            // Segundo, debe tener el parámetro activado (Sí/No)
            Parameter p = wall.LookupParameter(ParameterName);
            if (p != null && p.StorageType == StorageType.Integer)
            {
                return p.AsInteger() == 1;
            }

            return false;
        }

        private void EnsureParameterExists(Document doc)
        {
            // Verificamos si ya existe el parámetro de proyecto
            BindingMap bindingMap = doc.ParameterBindings;
            DefinitionBindingMapIterator it = bindingMap.ForwardIterator();
            bool exists = false;
            while (it.MoveNext())
            {
                if (it.Key.Name == ParameterName)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                CreateProjectParameter(doc);
            }
        }

        private void CreateProjectParameter(Document doc)
        {
            // En un entorno ideal, usaríamos Shared Parameters. 
            // Para simplificar esta herramienta "Plug & Play", intentaremos crear un Project Parameter
            // Nota: La creación de parámetros requiere una transacción. 
            // Como esto se llama desde el Handler (que ya está en Execute), deberíamos manejarlo con cuidado.
            // Pero el Handler de ScanWalls no tiene transacción abierta por defecto en el switch,
            // así que la abrimos aquí si es necesario.
            
            using (Transaction t = new Transaction(doc, "Crear Parámetro MP_GenerarFachada"))
            {
                t.Start();
                try
                {
                    // Crear categoría para Muros
                    Category wallCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Walls);
                    CategorySet catSet = doc.Application.Create.NewCategorySet();
                    catSet.Insert(wallCat);

                    // Definición (Revit 2026 usa ForgeTypeId)
                    ExternalDefinitionCreationOptions opt = new ExternalDefinitionCreationOptions(ParameterName, SpecTypeId.Boolean.YesNo);
                    
                    // Nota: Crear parámetros de proyecto programáticamente es complejo sin un archivo de compartidos.
                    // Para esta versión, informaremos al usuario o usaremos un método simplificado.
                    // Revit API no permite crear Project Parameters puramente "en memoria" sin Shared Parameter File fácilmente.
                    
                    // Fallback: Si no existe, el usuario debe crearlo o nosotros inyectamos uno temporal si podemos.
                    // Por ahora, asumiremos que si no existe, el escaneo simplemente no devolverá nada y el log avisará.
                }
                catch { }
                t.RollBack(); // No queremos forzar cambios raros sin archivo de compartidos
            }
        }
    }
}
