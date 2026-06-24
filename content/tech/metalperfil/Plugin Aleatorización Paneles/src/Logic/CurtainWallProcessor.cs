using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace PluginAleatorizar.Logic
{
    public class CurtainWallProcessor
    {
        private const double MmToFt = 1.0 / 304.8;

        public void Process(
            Document doc,
            Wall wall,
            IList<PanelTypeInfo> sequence,
            double solapeMm,
            Action<string> log,
            Action<double> reportProgress = null)
        {
            var curtainGrid = wall.CurtainGrid;
            if (curtainGrid == null)
            {
                log($"  [!] Muro {wall.Id}: no es un muro cortina. Saltando.");
                return;
            }

            if (sequence.Count == 0)
            {
                log($"  [!] Muro {wall.Id}: secuencia vacía. Saltando.");
                return;
            }

            var locCurve = (LocationCurve)wall.Location;
            double wallLengthFt = locCurve.Curve.Length;
            double wallLengthMm = wallLengthFt * 304.8;

            XYZ p0 = locCurve.Curve.GetEndPoint(0);
            XYZ p1 = locCurve.Curve.GetEndPoint(1);
            XYZ wallDir = (p1 - p0).Normalize();

            double baseZ = p0.Z;
            var heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
            double heightFt = heightParam?.AsDouble() ?? 3.0;
            double midZ = baseZ + heightFt * 0.5;

            // Cada celda mide el ancho real del perfil menos el solape
            double totalNominalMm = sequence.Sum(p => p.WidthMm - solapeMm);
            double extraMm = wallLengthMm - totalNominalMm;
            var widthsMm = sequence.Select(p => p.WidthMm - solapeMm).ToArray();

            if (extraMm > 1)
                log($"  Muro {wall.Id}: junta final {extraMm:F0} mm en la última celda " +
                    $"(con solape de {solapeMm:F0} mm aplicado).");
            else if (extraMm < -1)
                log($"  [!] Muro {wall.Id}: las piezas suman {-extraMm:F0} mm más que el muro; " +
                    "la última celda se recortará.");

            var positions = new List<double>();
            double cumFt = 0;
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                cumFt += widthsMm[i] * MmToFt;
                if (cumFt < wallLengthFt - 0.001)
                    positions.Add(cumFt);
            }

            // --- PASO 1: REJILLAS ---
            reportProgress?.Invoke(10);
            
            if (wall.Pinned)
            {
                try { wall.Pinned = false; } catch { }
            }

            // Borrado robusto: las rejillas pineadas (o bloqueadas por el tipo)
            // necesitan despinear + regenerar antes de poder eliminarse, y al
            // borrar pueden aparecer/cambiar ids, así que se itera en pasadas.
            for (int pass = 0; pass < 4; pass++)
            {
                curtainGrid = wall.CurtainGrid;
                var gridIds = curtainGrid.GetVGridLineIds()
                    .Concat(curtainGrid.GetUGridLineIds())
                    .ToList();
                if (gridIds.Count == 0) break;

                foreach (var id in gridIds)
                {
                    if (doc.GetElement(id) is CurtainGridLine gl)
                    {
                        try { if (gl.Pinned) gl.Pinned = false; } catch { }
                        try { if (gl.Lock) gl.Lock = false; } catch { }
                    }
                }
                doc.Regenerate();

                int deleted = 0;
                foreach (var id in gridIds)
                {
                    try
                    {
                        var res = doc.Delete(id);
                        if (res != null && res.Count > 0) deleted++;
                    }
                    catch { }
                }
                doc.Regenerate();

                if (deleted == 0) break;
            }

            curtainGrid = wall.CurtainGrid;
            int survivors = curtainGrid.GetVGridLineIds().Count
                          + curtainGrid.GetUGridLineIds().Count;
            if (survivors > 0)
                log($"  [!] Muro {wall.Id}: {survivors} rejilla(s) no se pudieron eliminar " +
                    "(probablemente impuestas por el tipo de muro). La distribución puede verse afectada.");

            foreach (double posFt in positions)
            {
                XYZ point = new XYZ(p0.X + wallDir.X * posFt, p0.Y + wallDir.Y * posFt, midZ);
                try { curtainGrid.AddGridLine(false, point, false); } catch { }
            }
            doc.Regenerate();

            // --- PASO 2: PANELES (La "segunda vuelta" que funciona) ---
            reportProgress?.Invoke(50);
            
            var panelIds = curtainGrid.GetPanelIds().ToList();
            var panels = panelIds
                .Select(id => doc.GetElement(id))
                .Where(e => e != null && e.get_BoundingBox(null) != null)
                .OrderBy(e =>
                {
                    var bbox = e.get_BoundingBox(null)!;
                    XYZ center = (bbox.Min + bbox.Max) * 0.5;
                    return center.DotProduct(wallDir);
                })
                .ToList();

            if (panels.Count != sequence.Count)
                log($"  [!] Muro {wall.Id}: {panels.Count} paneles tras regenerar, " +
                    $"{sequence.Count} previstos. Revisar rejillas no eliminadas.");

            int assigned = 0;
            int limit = Math.Min(panels.Count, sequence.Count);
            
            for (int i = 0; i < limit; i++)
            {
                var panel = panels[i];
                var symbolId = sequence[i].SymbolId;
                try
                {
                    if (panel.Pinned) panel.Pinned = false;
                    panel.ChangeTypeId(symbolId);
                    assigned++;
                }
                catch { }

                if (i % 5 == 0) reportProgress?.Invoke(50 + (40.0 * i / limit));
            }

            reportProgress?.Invoke(100);

            log($"  OK  {assigned}/{panels.Count} paneles asignados. " +
                $"Muro: {wallLengthMm:F0} mm");
        }
    }
}
