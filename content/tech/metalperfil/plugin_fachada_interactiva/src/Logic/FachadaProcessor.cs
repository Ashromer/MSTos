using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using PluginFachadaInteractiva.Commands;

namespace PluginFachadaInteractiva.Logic
{
    public class FachadaProcessor
    {
        private const double MmToFt = 1.0 / 304.8;

        public void Process(
            Document doc,
            Wall wall,
            MainState state,
            Action<string> log,
            Action<double> reportProgress = null)
        {
            var curtainGrid = wall.CurtainGrid;
            if (curtainGrid == null) return;

            // 1. Generar secuencia aleatoria
            var activeTypes = state.PanelTypes.Where(t => t.Weight > 0).ToList();
            if (activeTypes.Count == 0) return;

            double wallLengthMm = ((LocationCurve)wall.Location).Curve.Length * 304.8;
            var generator = new PanelSequenceGenerator();
            
            // Semilla
            int s = 42;
            int.TryParse(state.SeedText, out s);
            Random rnd = new Random(s);

            // Solape
            double solapeMm = 40.0;
            if (double.TryParse(state.SolapeText, out double solVal))
            {
                solapeMm = solVal;
            }

            double remainder;
            var sequence = generator.Generate(
                activeTypes.Select(t => t.ToInfo()).ToList(),
                wallLengthMm, out remainder, rnd.Next(), solapeMm);

            // 2. Recrear rejillas
            RecreateGrids(doc, wall, sequence, solapeMm, log, reportProgress);

            // 3. Asignar tipos y materiales
            AssignPanels(doc, wall, sequence, state, log, reportProgress);
        }

        private void RecreateGrids(Document doc, Wall wall, IList<PanelTypeInfo> sequence, double solapeMm, Action<string> log, Action<double> reportProgress)
        {
            var curtainGrid = wall.CurtainGrid;
            var locCurve = (LocationCurve)wall.Location;
            XYZ p0 = locCurve.Curve.GetEndPoint(0);
            XYZ p1 = locCurve.Curve.GetEndPoint(1);
            XYZ wallDir = (p1 - p0).Normalize();
            double wallLengthFt = locCurve.Curve.Length;

            var heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
            double heightFt = heightParam?.AsDouble() ?? 3.0;
            double midZ = p0.Z + heightFt * 0.5;

            var positions = new List<double>();
            double cumFt = 0;
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                cumFt += (sequence[i].WidthMm - solapeMm) * MmToFt;
                if (cumFt < wallLengthFt - 0.001)
                    positions.Add(cumFt);
            }

            // Borrado de rejillas existentes
            for (int pass = 0; pass < 2; pass++)
            {
                var gridIds = wall.CurtainGrid.GetVGridLineIds().Concat(wall.CurtainGrid.GetUGridLineIds()).ToList();
                foreach (var id in gridIds)
                {
                    if (doc.GetElement(id) is CurtainGridLine gl)
                    {
                        if (gl.Pinned) gl.Pinned = false;
                    }
                    try { doc.Delete(id); } catch { }
                }
                doc.Regenerate();
            }

            // Añadir nuevas rejillas
            foreach (double posFt in positions)
            {
                XYZ point = new XYZ(p0.X + wallDir.X * posFt, p0.Y + wallDir.Y * posFt, midZ);
                try { wall.CurtainGrid.AddGridLine(false, point, false); } catch { }
            }
            doc.Regenerate();
        }

        private void AssignPanels(Document doc, Wall wall, IList<PanelTypeInfo> sequence, MainState state, Action<string> log, Action<double> reportProgress)
        {
            var locCurve = (LocationCurve)wall.Location;
            XYZ wallDir = (locCurve.Curve.GetEndPoint(1) - locCurve.Curve.GetEndPoint(0)).Normalize();
            
            var panelIds = wall.CurtainGrid.GetPanelIds().ToList();
            var panels = panelIds
                .Select(id => doc.GetElement(id))
                .Where(e => e != null)
                .OrderBy(e =>
                {
                    var bbox = e.get_BoundingBox(null);
                    if (bbox == null) return 0.0;
                    XYZ center = (bbox.Min + bbox.Max) * 0.5;
                    return center.DotProduct(wallDir);
                })
                .ToList();

            int limit = Math.Min(panels.Count, sequence.Count);
            for (int i = 0; i < limit; i++)
            {
                var panel = panels[i];
                var typeInfo = sequence[i];
                
                try
                {
                    if (panel.Pinned) panel.Pinned = false;
                    panel.ChangeTypeId(typeInfo.SymbolId);
                    
                    // --- Lógica de Materiales ---
                    ApplyMaterial(panel, typeInfo, state, doc);
                }
                catch (Exception ex)
                {
                    log($"  [!] Error asignando panel {i}: {ex.Message}");
                }
            }
        }

        private void ApplyMaterial(Element panel, PanelTypeInfo typeInfo, MainState state, Document doc)
        {
            ElementId matId = ElementId.InvalidElementId;

            if (state.UseGlobalMaterial && state.GlobalMaterialId != null)
            {
                matId = state.GlobalMaterialId;
            }
            else
            {
                // Buscar el material asignado a este tipo en el UI
                var uiType = state.PanelTypes.FirstOrDefault(t => t.SymbolId == typeInfo.SymbolId);
                if (uiType != null && uiType.MaterialId != null)
                {
                    matId = uiType.MaterialId;
                }
            }

            if (matId != ElementId.InvalidElementId)
            {
                // Intentar asignar a parámetro "Material" (común en paneles)
                Parameter p = panel.LookupParameter("Material");
                if (p == null) p = panel.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM);
                
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(matId);
                }
                else
                {
                    // Si no es parámetro de instancia, podríamos intentar en el tipo,
                    // pero eso afectaría a todos los paneles del mismo tipo.
                    // Dado que el usuario quiere control "por tipo" o "global", esto suele ser aceptable.
                    var symbol = doc.GetElement(panel.GetTypeId()) as FamilySymbol;
                    if (symbol != null)
                    {
                        Parameter ps = symbol.LookupParameter("Material");
                        if (ps != null && !ps.IsReadOnly) ps.Set(matId);
                    }
                }
            }
        }
    }
}
