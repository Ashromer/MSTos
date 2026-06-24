using Autodesk.Revit.DB;

namespace PluginFachadaInteractiva.Logic
{
    public static class FamilyWidthReader
    {
        private const double FtToMm = 304.8;

        /// <summary>
        /// Lee el ancho del perfil en mm desde la geometría de la FamilySymbol cargada en el proyecto.
        /// Activa el símbolo si es necesario (requiere una transacción activa o abre una propia).
        /// </summary>
        public static double GetWidthMm(Document doc, FamilySymbol symbol)
        {
            EnsureActivated(doc, symbol);

            var opts = new Options
            {
                ComputeReferences = false,
                DetailLevel = ViewDetailLevel.Fine,
                IncludeNonVisibleObjects = false
            };

            var geomElem = symbol.get_Geometry(opts);
            if (geomElem == null) return 0;

            double minX = double.MaxValue;
            double maxX = double.MinValue;

            foreach (var obj in geomElem)
                CollectXBounds(obj, ref minX, ref maxX);

            return minX >= maxX ? 0 : (maxX - minX) * FtToMm;
        }

        private static void EnsureActivated(Document doc, FamilySymbol symbol)
        {
            if (symbol.IsActive) return;

            if (doc.IsModifiable)
            {
                symbol.Activate();
                doc.Regenerate();
                return;
            }

            using var t = new Transaction(doc, "Activar símbolo");
            t.Start();
            symbol.Activate();
            doc.Regenerate();
            t.Commit();
        }

        private static void CollectXBounds(GeometryObject obj, ref double minX, ref double maxX)
        {
            if (obj is Solid solid && solid.Volume > 1e-9)
            {
                foreach (Face face in solid.Faces)
                {
                    var mesh = face.Triangulate();
                    foreach (XYZ v in mesh.Vertices)
                    {
                        if (v.X < minX) minX = v.X;
                        if (v.X > maxX) maxX = v.X;
                    }
                }
            }
            else if (obj is GeometryInstance inst)
            {
                foreach (var inner in inst.GetInstanceGeometry())
                    CollectXBounds(inner, ref minX, ref maxX);
            }
        }
    }
}
