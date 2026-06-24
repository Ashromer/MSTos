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
            Action<string> log)
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

            // Altura media del muro para el punto de anclaje de las líneas de rejilla
            double baseZ = p0.Z;
            var heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
            double heightFt = heightParam?.AsDouble() ?? 3.0;
            double midZ = baseZ + heightFt * 0.5;

            // Posiciones de las líneas de rejilla (entre paneles, no en el extremo final)
            var positions = new List<double>();
            double cumFt = 0;
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                cumFt += sequence[i].WidthMm * MmToFt;
                if (cumFt < wallLengthFt - 0.001)
                    positions.Add(cumFt);
            }

            using var t = new Transaction(doc, $"Aleatorizar paneles — Muro {wall.Id}");
            t.Start();

            // 0. Comprobar si el muro está anclado
            if (wall.Pinned)
            {
                try
                {
                    wall.Pinned = false;
                    log($"  [i] Muro {wall.Id} estaba anclado. Se ha desanclado para procesar.");
                }
                catch (Exception)
                {
                    log($"  [!] Muro {wall.Id} está anclado y no se puede desanclar. Es probable que la operación falle.");
                }
            }

            // 1. Eliminar las líneas de rejilla existentes (U y V)
            var vIds = curtainGrid.GetVGridLineIds().ToList();
            var uIds = curtainGrid.GetUGridLineIds().ToList();
            var allGridIds = vIds.Concat(uIds).ToList();

            foreach (var id in allGridIds)
            {
                var gl = doc.GetElement(id);
                if (gl != null && gl.Pinned) 
                {
                    try { gl.Pinned = false; } catch { }
                }
            }
            
            if (allGridIds.Count > 0)
            {
                try 
                { 
                    doc.Delete(allGridIds); 
                    doc.Regenerate(); // Regenerar tras borrar para limpiar el estado
                }
                catch (Exception ex) 
                { 
                    log($"  [!] Error al eliminar líneas existentes: {ex.Message}"); 
                    // Si falla el borrado masivo, intentamos uno a uno
                    foreach(var id in allGridIds)
                    {
                        try { doc.Delete(id); } catch { }
                    }
                    doc.Regenerate();
                }
            }

            // 2. Añadir nuevas líneas de rejilla en las posiciones calculadas
            foreach (double posFt in positions)
            {
                XYZ point = new XYZ(
                    p0.X + wallDir.X * posFt,
                    p0.Y + wallDir.Y * posFt,
                    midZ);
                try 
                { 
                    curtainGrid.AddGridLine(false, point, false); 
                }
                catch (Exception ex) 
                { 
                    log($"  [!] Error añadiendo línea en {posFt * 304.8:F0} mm: {ex.Message}"); 
                }
            }
            doc.Regenerate();

            // 3. Ordenar paneles de izquierda a derecha y asignar tipos
            var panelIds = curtainGrid.GetPanelIds().ToList();

            int expectedPanels = positions.Count + 1;
            if (panelIds.Count != expectedPanels)
                log($"  [!] Se esperaban {expectedPanels} paneles, se encontraron {panelIds.Count}.");

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

            // Activar símbolos
            foreach (var info in sequence.DistinctBy(s => s.SymbolId))
            {
                if (doc.GetElement(info.SymbolId) is FamilySymbol symbol && !symbol.IsActive)
                {
                    symbol.Activate();
                }
            }
            doc.Regenerate();

            int assigned = 0;
            int limit = Math.Min(panels.Count, sequence.Count);
            var failures = new List<(Element Panel, ElementId SymbolId, int Index)>();

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
                catch (Exception)
                {
                    failures.Add((panel, symbolId, i));
                }
            }

            // 4. Segundo intento (Retry Logic) para paneles fallidos
            if (failures.Count > 0)
            {
                log($"  [i] Intentando reasignar {failures.Count} paneles que fallaron en el primer pase...");
                doc.Regenerate(); // Forzar actualización del estado de Revit

                int retrySuccess = 0;
                foreach (var fail in failures)
                {
                    try
                    {
                        if (fail.Panel.Pinned) fail.Panel.Pinned = false;
                        fail.Panel.ChangeTypeId(fail.SymbolId);
                        retrySuccess++;
                        assigned++;
                    }
                    catch (Exception ex)
                    {
                        log($"  [!] Panel {fail.Index} (ID: {fail.Panel.Id}): fallo definitivo — {ex.Message}");
                    }
                }
                if (retrySuccess > 0)
                    log($"  [+] Recuperados {retrySuccess} paneles en el segundo intento.");
            }

            t.Commit();

            double totalSeqMm = sequence.Sum(p => p.WidthMm);
            log($"  OK  {assigned}/{panels.Count} paneles asignados. " +
                $"Ancho secuencia: {totalSeqMm:F0} mm / Muro: {wallLengthMm:F0} mm " +
                $"(dif: {totalSeqMm - wallLengthMm:+0.#;-0.#;0} mm)");
        }
    }
}
