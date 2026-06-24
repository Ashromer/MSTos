using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginFamilias.CAD;

namespace PluginFamilias.Core
{
    public class FamilyGenerator
    {
        private readonly UIApplication _uiApp;
        private const double GrosorMm  = 2.0;
        private const double AlturaMm  = 3000.0;
        private const double MmToFt    = 1.0 / 304.8;

        // Teselación adaptativa por sagita: el ángulo por segmento se elige para que
        // la desviación cuerda-arco no supere SagittaMm, acotado entre Min y Max.
        // Radios pequeños (pliegues) admiten ángulos grandes → muchos menos segmentos.
        private const double SagittaMm    = 0.2;
        private const double MinArcSegDeg = 12.0;
        private const double MaxArcSegDeg = 40.0;

        // Grosores a intentar. Las tapas del contorno miden = grosor, así que
        // ningún fallback puede bajar de la ShortCurveTolerance de Revit (~0.8 mm)
        private static readonly double[] ThicknessFallbacks = { 2.0, 1.5, 1.0 };

        public FamilyGenerator(UIApplication uiApp) => _uiApp = uiApp;

        private double Ft(double mm) => mm * MmToFt;

        // ── Punto de entrada ─────────────────────────────────────────────────────

        public void GenerateBatch(PolylineData data, string templatePath, string outputFolder,
            Action<string>? detail = null)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Plantilla no encontrada: " + templatePath);

            Document famDoc = _uiApp.Application.NewFamilyDocument(templatePath);

            try
            {
                var norm = Normalize(data.Vertices);

                // Tolerancia mínima de segmento en mm, con margen sobre la de Revit
                double tolMm = _uiApp.Application.ShortCurveTolerance * 304.8 * 1.2;

                // 1. Teselar arcos → polilínea de solo puntos
                // 2. Limpiar: fusionar puntos próximos y eliminar colineales
                var basePts = Tessellate(norm, tolMm);
                int afterTess = basePts.Count;
                basePts = CleanPolyline(basePts, tolMm);

                detail?.Invoke($"  Geometría: {data.Vertices.Count} vértices DXF → {afterTess} teselados → {basePts.Count} tras limpieza (tol {tolMm:F2} mm)");

                // Enderezar: la línea base inferior del perfil queda horizontal
                basePts = AlignToBaseLine(basePts, detail);

                if (basePts.Count < 2)
                    throw new InvalidOperationException("Perfil degenerado tras la limpieza de vértices.");

                // Recoger planos de referencia una sola vez
                var planes = new FilteredElementCollector(famDoc)
                    .OfClass(typeof(ReferencePlane))
                    .Cast<ReferencePlane>()
                    .Where(p => !string.IsNullOrEmpty(p.Name))
                    .ToDictionary(p => p.Name, p => p);

                Extrusion? extr = null;
                double usedThickness = GrosorMm;

                using (Transaction t = new Transaction(famDoc, "Crear Perfil"))
                {
                    t.Start();

                    // Tragar warnings (aviso de rendimiento, etc.) para que el batch
                    // no se detenga pidiendo OK por cada familia
                    var fho = t.GetFailureHandlingOptions();
                    fho.SetFailuresPreprocessor(new WarningSwallower());
                    t.SetFailureHandlingOptions(fho);

                    // — Parámetros —————————————————————————————————————————————
                    FamilyManager mgr = famDoc.FamilyManager;

                    FamilyParameter pGrosor = mgr.get_Parameter("Grosor")
                        ?? mgr.AddParameter("Grosor", GroupTypeId.Geometry, SpecTypeId.Length, false)
                        ?? throw new InvalidOperationException("No se pudo crear 'Grosor'.");
                    FamilyParameter pAltura = mgr.get_Parameter("Altura")
                        ?? mgr.AddParameter("Altura", GroupTypeId.Geometry, SpecTypeId.Length, false)
                        ?? throw new InvalidOperationException("No se pudo crear 'Altura'.");

                    FamilyParameter? pMaterial = null;
                    try { pMaterial = mgr.get_Parameter("Material del Panel") ?? mgr.AddParameter("Material del Panel", GroupTypeId.Materials, SpecTypeId.Reference.Material, false); }
                    catch { }

                    mgr.NewType(data.TypeName);
                    mgr.Set(pGrosor, Ft(GrosorMm));
                    mgr.Set(pAltura, Ft(AlturaMm));

                    // — Mover planos de referencia ANTES de la extrusión ————————
                    AdjustReferencePlanes(famDoc, planes, basePts, AlturaMm);

                    // — Extrusión con reintento por grosor decreciente ——————————
                    var sketchPlane = SketchPlane.Create(famDoc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));
                    Exception? lastExtrusionException = null;

                    foreach (double thickness in ThicknessFallbacks)
                    {
                        using (SubTransaction st = new SubTransaction(famDoc))
                        {
                            st.Start();
                            try
                            {
                                var off  = OffsetOpenPolyline(basePts, thickness);
                                var arr  = BuildClosedLoop(basePts, off, tolMm);
                                var prof = new CurveArrArray();
                                prof.Append(arr);
                                extr = famDoc.FamilyCreate.NewExtrusion(true, prof, sketchPlane, Ft(AlturaMm));
                                usedThickness = thickness;
                                st.Commit();
                                break;
                            }
                            catch (Exception ex)
                            {
                                lastExtrusionException = ex;
                                detail?.Invoke($"  [intento {thickness} mm] {ex.Message}");
                                st.RollBack();
                            }
                        }
                    }

                    if (extr == null)
                        throw new InvalidOperationException(
                            $"Perfil inválido con todos los grosores probados. " +
                            $"Puntos del contorno: {basePts.Count}. Último error: {lastExtrusionException?.Message}",
                            lastExtrusionException);

                    // — Asociar parámetros a la extrusión ———————————————————————
                    var pEnd = extr.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM);
                    if (pEnd != null) mgr.AssociateElementParameterToFamilyParameter(pEnd, pAltura);

                    if (pMaterial != null)
                    {
                        var pExtrMat = extr.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                        if (pExtrMat != null) mgr.AssociateElementParameterToFamilyParameter(pExtrMat, pMaterial);
                    }

                    // — Pinear cara superior al plano "Top" ————————————————————
                    PinTopFace(famDoc, extr, planes);

                    // — Anclar SOLO la cara izquierda al plano "Left" (derecha libre) ─
                    PinLeftFace(famDoc, extr, planes);

                    // — Subcategoría (no crítico) ———————————————————————————————
                    try
                    {
                        var category = famDoc.OwnerFamily.FamilyCategory;
                        Category subcat = category.SubCategories.Contains("Paneles de Fachada")
                            ? category.SubCategories.get_Item("Paneles de Fachada")
                            : famDoc.Settings.Categories.NewSubcategory(category, "Paneles de Fachada");
                        var pSubcat = extr.get_Parameter(BuiltInParameter.FAMILY_ELEM_SUBCATEGORY);
                        if (pSubcat != null && subcat != null) pSubcat.Set(subcat.Id);
                    }
                    catch { }

                    t.Commit();
                }

                // — Guardar ————————————————————————————————————————————————————
                string outDir = Path.Combine(outputFolder, data.FamilyName);
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                string safe = $"{Clean(data.FamilyName)}_{Clean(data.TypeName)}";
                if (usedThickness < GrosorMm)
                    safe += $"_g{usedThickness}mm";

                famDoc.SaveAs(Path.Combine(outDir, safe + ".rfa"),
                    new SaveAsOptions { OverwriteExistingFile = true });
            }
            finally
            {
                if (famDoc.IsValidObject) famDoc.Close(false);
            }
        }

        // ── Pinear cara superior al plano "Top" ──────────────────────────────────

        private void PinTopFace(Document famDoc, Extrusion extr,
            Dictionary<string, ReferencePlane> planes)
        {
            try
            {
                if (!planes.TryGetValue("Top", out var topPlane)) return;

                Autodesk.Revit.DB.View? view = new FilteredElementCollector(famDoc)
                    .OfClass(typeof(Autodesk.Revit.DB.View))
                    .Cast<Autodesk.Revit.DB.View>()
                    .FirstOrDefault(v => v.ViewType == ViewType.Elevation && !v.IsTemplate)
                    ?? new FilteredElementCollector(famDoc)
                        .OfClass(typeof(Autodesk.Revit.DB.View))
                        .Cast<Autodesk.Revit.DB.View>()
                        .FirstOrDefault(v => !v.IsTemplate
                            && v.ViewType != ViewType.Schedule
                            && v.ViewType != ViewType.DrawingSheet);

                if (view is null) return;

                var opt = new Options { ComputeReferences = true, View = view };
                Reference? topFaceRef = null;

                foreach (GeometryObject gObj in extr.get_Geometry(opt))
                {
                    if (gObj is not Solid solid) continue;
                    foreach (Face face in solid.Faces)
                    {
                        if (face is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
                        {
                            topFaceRef = pf.Reference;
                            break;
                        }
                    }
                    if (topFaceRef != null) break;
                }

                if (topFaceRef is null) return;

                famDoc.FamilyCreate.NewAlignment(view!, topPlane.GetReference(), topFaceRef);
            }
            catch { }
        }

        // ── Anclar la cara izquierda al plano "Left" ─────────────────────────────
        // Bloquea SOLO el borde izquierdo. La cara derecha queda libre, de modo
        // que el panel conserva su ancho fijo y no se deforma al colocarlo en una
        // celda de muro cortina algo más ancha o más estrecha. No crítico: si no
        // se encuentra una cara vertical limpia (±X) se omite sin fallar.
        private void PinLeftFace(Document famDoc, Extrusion extr,
            Dictionary<string, ReferencePlane> planes)
        {
            try
            {
                if (!planes.TryGetValue("Left", out var leftPlane)) return;

                Autodesk.Revit.DB.View? view = new FilteredElementCollector(famDoc)
                    .OfClass(typeof(Autodesk.Revit.DB.View))
                    .Cast<Autodesk.Revit.DB.View>()
                    .FirstOrDefault(v => v.ViewType == ViewType.FloorPlan && !v.IsTemplate)
                    ?? new FilteredElementCollector(famDoc)
                        .OfClass(typeof(Autodesk.Revit.DB.View))
                        .Cast<Autodesk.Revit.DB.View>()
                        .FirstOrDefault(v => !v.IsTemplate
                            && v.ViewType != ViewType.Schedule
                            && v.ViewType != ViewType.DrawingSheet);

                if (view is null) return;

                var opt = new Options { ComputeReferences = true, View = view };
                Reference? leftFaceRef = null;
                double bestX = double.MaxValue;

                foreach (GeometryObject gObj in extr.get_Geometry(opt))
                {
                    if (gObj is not Solid solid) continue;
                    foreach (Face face in solid.Faces)
                    {
                        if (face is not PlanarFace pf) continue;
                        // Cara vertical mirando en X (tapa izquierda del perfil)
                        if (Math.Abs(pf.FaceNormal.X) > 0.999 &&
                            Math.Abs(pf.FaceNormal.Y) < 1e-3 &&
                            Math.Abs(pf.FaceNormal.Z) < 1e-3 &&
                            pf.Origin.X < bestX)
                        {
                            bestX = pf.Origin.X;
                            leftFaceRef = pf.Reference;
                        }
                    }
                }

                if (leftFaceRef is null) return;
                famDoc.FamilyCreate.NewAlignment(view!, leftPlane.GetReference(), leftFaceRef);
            }
            catch { }
        }

        // ── Geometría ────────────────────────────────────────────────────────────

        private List<PolylineVertex> Normalize(List<PolylineVertex> vertices)
        {
            if (vertices.Count == 0) return vertices;
            double cx = (vertices.Min(v => v.X) + vertices.Max(v => v.X)) / 2.0;
            double cy = (vertices.Min(v => v.Y) + vertices.Max(v => v.Y)) / 2.0;
            return vertices
                .Select(v => new PolylineVertex { X = v.X - cx, Y = v.Y - cy, Bulge = v.Bulge })
                .ToList();
        }

        // Convierte la polilínea (rectas + arcos bulge) en una cadena de puntos:
        // los arcos se teselan en segmentos de como máximo MaxArcSegDeg de barrido,
        // con cuerda mínima por encima de la tolerancia de Revit.
        public static List<(double X, double Y)> Tessellate(List<PolylineVertex> v, double tolMm)
        {
            var pts = new List<(double X, double Y)>();
            int n = v.Count;

            for (int i = 0; i < n - 1; i++)
            {
                var a = v[i];
                var b = v[i + 1];
                pts.Add((a.X, a.Y));

                if (Math.Abs(a.Bulge) < 1e-6) continue;

                // Punto medio del arco con la convención del proyecto (cb = -bulge)
                double cb = -a.Bulge;
                double mx = (a.X + b.X) / 2.0 - cb * (b.Y - a.Y) / 2.0;
                double my = (a.Y + b.Y) / 2.0 + cb * (b.X - a.X) / 2.0;

                var (ccx, ccy) = Circumcenter(a.X, a.Y, mx, my, b.X, b.Y);
                if (double.IsNaN(ccx)) continue; // degenerado → tratar como recta

                double r  = Math.Sqrt((a.X - ccx) * (a.X - ccx) + (a.Y - ccy) * (a.Y - ccy));
                double a1 = Math.Atan2(a.Y - ccy, a.X - ccx);
                double am = Math.Atan2(my - ccy, mx - ccx);
                double a2 = Math.Atan2(b.Y - ccy, b.X - ccx);

                double sweep  = SignedSweep(a1, am, a2);
                double arcLen = Math.Abs(sweep) * r;
                double minChord = Math.Max(tolMm * 2.0, 1.0);

                double sagAngle = r > SagittaMm
                    ? 2.0 * Math.Acos(1.0 - SagittaMm / r)
                    : MaxArcSegDeg * Math.PI / 180.0;
                double segAngle = Math.Clamp(sagAngle,
                    MinArcSegDeg * Math.PI / 180.0,
                    MaxArcSegDeg * Math.PI / 180.0);

                int nSeg = (int)Math.Ceiling(Math.Abs(sweep) / segAngle);
                int maxSeg = Math.Max(1, (int)(arcLen / minChord));
                nSeg = Math.Max(1, Math.Min(nSeg, maxSeg));

                for (int k = 1; k < nSeg; k++)
                {
                    double ang = a1 + sweep * k / nSeg;
                    pts.Add((ccx + r * Math.Cos(ang), ccy + r * Math.Sin(ang)));
                }
            }

            pts.Add((v[n - 1].X, v[n - 1].Y));
            return pts;
        }

        // Barrido firmado de a1 a a2 pasando por am (positivo = antihorario)
        private static double SignedSweep(double a1, double am, double a2)
        {
            double TwoPi = 2.0 * Math.PI;
            double dm = ((am - a1) % TwoPi + TwoPi) % TwoPi;
            double d2 = ((a2 - a1) % TwoPi + TwoPi) % TwoPi;
            return dm <= d2 ? d2 : d2 - TwoPi;
        }

        private static (double cx, double cy) Circumcenter(
            double ax, double ay, double bx, double by, double cx, double cy)
        {
            double d = 2.0 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            if (Math.Abs(d) < 1e-12) return (double.NaN, double.NaN);
            double ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
            double uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;
            return (ux, uy);
        }

        // Limpieza: fusiona puntos más próximos que minSegMm y elimina colineales.
        // La desviación colineal (0.02 mm) es muy inferior a la sagita de los arcos
        // teselados, así que la forma curva se conserva.
        private static List<(double X, double Y)> CleanPolyline(
            List<(double X, double Y)> pts, double minSegMm)
        {
            if (pts.Count < 3) return pts;

            var merged = new List<(double X, double Y)> { pts[0] };
            for (int i = 1; i < pts.Count - 1; i++)
                if (Dist(merged[^1], pts[i]) >= minSegMm) merged.Add(pts[i]);

            if (Dist(merged[^1], pts[^1]) >= minSegMm) merged.Add(pts[^1]);
            else if (merged.Count > 1) merged[^1] = pts[^1];
            else merged.Add(pts[^1]);

            if (merged.Count < 3) return merged;

            const double colTol = 0.02;
            var result = new List<(double X, double Y)> { merged[0] };
            for (int i = 1; i < merged.Count - 1; i++)
            {
                var a = result[^1];
                var b = merged[i];
                var c = merged[i + 1];
                double ux = c.X - a.X, uy = c.Y - a.Y;
                double len = Math.Sqrt(ux * ux + uy * uy);
                double dev = len < 1e-9 ? 0 : Math.Abs((b.X - a.X) * uy - (b.Y - a.Y) * ux) / len;
                if (dev > colTol) result.Add(b);
            }
            result.Add(merged[^1]);
            return result;
        }

        // Offset de polilínea abierta de solo rectas:
        // cada segmento se desplaza por su normal y los vértices interiores se
        // resuelven por intersección real de los dos segmentos desplazados
        // (con recorte a miterLimit en esquinas muy agudas).
        private static List<(double X, double Y)> OffsetOpenPolyline(
            List<(double X, double Y)> p, double d, double miterLimit = 4.0)
        {
            int n = p.Count;
            var dirX = new double[n - 1];
            var dirY = new double[n - 1];
            var nX   = new double[n - 1];
            var nY   = new double[n - 1];

            for (int i = 0; i < n - 1; i++)
            {
                double dx = p[i + 1].X - p[i].X;
                double dy = p[i + 1].Y - p[i].Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-9)
                {
                    dirX[i] = i > 0 ? dirX[i - 1] : 1.0;
                    dirY[i] = i > 0 ? dirY[i - 1] : 0.0;
                }
                else
                {
                    dirX[i] = dx / len;
                    dirY[i] = dy / len;
                }
                nX[i] = -dirY[i];
                nY[i] = dirX[i];
            }

            var off = new List<(double X, double Y)>(n)
            {
                (p[0].X + d * nX[0], p[0].Y + d * nY[0])
            };

            for (int i = 1; i < n - 1; i++)
            {
                double ax = p[i].X + d * nX[i - 1], ay = p[i].Y + d * nY[i - 1];
                double bx = p[i].X + d * nX[i],     by = p[i].Y + d * nY[i];

                double cross = dirX[i - 1] * dirY[i] - dirY[i - 1] * dirX[i];

                if (Math.Abs(cross) < 1e-9)
                {
                    off.Add(((ax + bx) / 2.0, (ay + by) / 2.0));
                    continue;
                }

                double t  = ((bx - ax) * dirY[i] - (by - ay) * dirX[i]) / cross;
                double ix = ax + t * dirX[i - 1];
                double iy = ay + t * dirY[i - 1];

                double miterLen = Math.Sqrt((ix - p[i].X) * (ix - p[i].X) + (iy - p[i].Y) * (iy - p[i].Y));
                if (miterLen > miterLimit * Math.Abs(d))
                {
                    double bxn = nX[i - 1] + nX[i], byn = nY[i - 1] + nY[i];
                    double mag = Math.Sqrt(bxn * bxn + byn * byn);
                    if (mag < 1e-9)
                        off.Add(((ax + bx) / 2.0, (ay + by) / 2.0));
                    else
                        off.Add((p[i].X + miterLimit * d * bxn / mag, p[i].Y + miterLimit * d * byn / mag));
                }
                else
                {
                    off.Add((ix, iy));
                }
            }

            off.Add((p[n - 1].X + d * nX[n - 2], p[n - 1].Y + d * nY[n - 2]));
            return off;
        }

        // Bucle cerrado: ida (original) + vuelta (offset invertido), las tapas
        // surgen del cierre implícito. Solo líneas rectas — sin arcos que puedan
        // degenerar. Limpieza circular final contra la tolerancia de Revit.
        private CurveArray BuildClosedLoop(
            List<(double X, double Y)> fwd, List<(double X, double Y)> off, double tolMm)
        {
            // Forzar que las tapas de los extremos (caps) sean perfectamente verticales (dx = 0)
            // para que las caras extremas de la extrusión sean paralelas al plano Left (X=cte).
            if (off.Count > 0 && fwd.Count > 0)
            {
                off[0] = (fwd[0].X, off[0].Y);
                off[off.Count - 1] = (fwd[fwd.Count - 1].X, off[off.Count - 1].Y);
            }

            var loop = new List<(double X, double Y)>(fwd.Count + off.Count);
            loop.AddRange(fwd);
            for (int i = off.Count - 1; i >= 0; i--) loop.Add(off[i]);

            var clean = new List<(double X, double Y)> { loop[0] };
            for (int i = 1; i < loop.Count; i++)
                if (Dist(clean[^1], loop[i]) >= tolMm) clean.Add(loop[i]);
            while (clean.Count > 2 && Dist(clean[^1], clean[0]) < tolMm)
                clean.RemoveAt(clean.Count - 1);

            if (clean.Count < 3)
                throw new InvalidOperationException("Contorno degenerado tras la limpieza final.");

            // Apoyar la BASE INFERIOR del perfil sobre el plano Center (Front/Back):
            // el punto más bajo en planta queda tocando Y=0 y toda la onda sobresale
            // por encima (hacia +Y = exterior). Así la cara interior/inferior asienta
            // en el plano de referencia de la familia de muro cortina.
            double yMin = double.MaxValue;
            foreach (var pt in clean)
                if (pt.Y < yMin) yMin = pt.Y;
            if (Math.Abs(yMin) > 1e-9)
                for (int i = 0; i < clean.Count; i++)
                    clean[i] = (clean[i].X, clean[i].Y - yMin);

            var arr = new CurveArray();
            for (int i = 0; i < clean.Count; i++)
            {
                var a = clean[i];
                var b = clean[(i + 1) % clean.Count];
                arr.Append(Line.CreateBound(
                    new XYZ(Ft(a.X), Ft(a.Y), 0),
                    new XYZ(Ft(b.X), Ft(b.Y), 0)));
            }
            return arr;
        }

        private static double Dist((double X, double Y) a, (double X, double Y) b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        // Endereza el perfil alineando su LÍNEA BASE INFERIOR con la horizontal.
        // La base es la recta sobre la que se apoya la chapa: se toma la banda
        // central (10%–90% del ancho, ignorando las colas/retornos de los extremos),
        // se obtiene su envolvente inferior (lower convex hull = los vértices más
        // bajos de la fibra neutra) y se ajusta una recta por mínimos cuadrados.
        // El perfil se rota para dejar esa recta paralela a X; el apoyo en Y=0 lo
        // hace después BuildClosedLoop (yMin→0).
        private static List<(double X, double Y)> AlignToBaseLine(
            List<(double X, double Y)> pts, Action<string>? detail)
        {
            if (pts.Count < 3) return pts;

            double yMin = pts.Min(p => p.Y);
            
            // Recoger los puntos de los valles (cercanos al Y mínimo, con margen de 2.0 mm)
            var valleys = pts.Where(p => p.Y <= yMin + 2.0).ToList();
            if (valleys.Count < 2) return pts;

            // Pendiente de la recta base por mínimos cuadrados sobre los puntos de los valles
            double mx = valleys.Average(p => p.X), my = valleys.Average(p => p.Y);
            double sxx = 0, sxy = 0;
            foreach (var p in valleys)
            {
                double dx = p.X - mx;
                sxx += dx * dx;
                sxy += dx * (p.Y - my);
            }
            double theta = Math.Abs(sxx) < 1e-12 ? 0.0 : Math.Atan2(sxy, sxx);
            double deg = theta * 180.0 / Math.PI;
            if (Math.Abs(deg) < 0.01) return pts;

            double cos = Math.Cos(-theta), sin = Math.Sin(-theta);
            var rotated = pts.Select(p =>
                (X: p.X * cos - p.Y * sin, Y: p.X * sin + p.Y * cos)).ToList();

            detail?.Invoke($"  Enderezado: {deg:F2}° (línea base inferior basada en valles)");

            double cx = (rotated.Min(p => p.X) + rotated.Max(p => p.X)) / 2.0;
            return rotated.Select(p => (p.X - cx, p.Y)).ToList();
        }

        // Envolvente inferior (lower convex hull, de izquierda a derecha): la cadena
        // de vértices que delimitan el conjunto por abajo. Andrew's monotone chain.
        private static List<(double X, double Y)> LowerHull(List<(double X, double Y)> pts)
        {
            var p = pts.Distinct().OrderBy(a => a.X).ThenBy(a => a.Y).ToList();
            if (p.Count < 3) return p;

            static double Cross((double X, double Y) o, (double X, double Y) a, (double X, double Y) b)
                => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

            var hull = new List<(double X, double Y)>();
            foreach (var pt in p)
            {
                while (hull.Count >= 2 && Cross(hull[^2], hull[^1], pt) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(pt);
            }
            return hull;
        }

        private void AdjustReferencePlanes(Document doc,
            Dictionary<string, ReferencePlane> planes,
            List<(double X, double Y)> pts, double heightMm)
        {
            double heightFt = Ft(heightMm);

            // Intentar ajustar mediante el parámetro de Ancho/Width de la familia para evitar romper restricciones EQ
            bool widthSet = false;
            FamilyManager mgr = doc.FamilyManager;
            FamilyParameter? pWidth = null;
            foreach (FamilyParameter fp in mgr.Parameters)
            {
                if (fp.Definition.Name == "Width" || fp.Definition.Name == "Ancho")
                {
                    pWidth = fp;
                    break;
                }
            }

            if (pWidth != null)
            {
                try
                {
                    double profileWidth = pts.Max(p => p.X) - pts.Min(p => p.X);
                    double largeWidth = profileWidth * 2.0 + 200.0; // 2x ancho + 200mm de holgura
                    mgr.Set(pWidth, Ft(largeWidth));
                    widthSet = true;
                }
                catch { }
            }

            // Si no se pudo ajustar por parámetro (o no existía), mover los planos manualmente
            if (!widthSet)
            {
                double xMinFt = Ft(pts.Min(p => p.X));
                double xMaxFt = Ft(pts.Max(p => p.X));

                if (planes.TryGetValue("Left", out var left))
                    ElementTransformUtils.MoveElement(doc, left.Id,
                        new XYZ(xMinFt - left.GetPlane().Origin.X, 0, 0));

                if (planes.TryGetValue("Right", out var right))
                {
                    double freeX = xMaxFt + (xMaxFt - xMinFt) + Ft(50.0);
                    ElementTransformUtils.MoveElement(doc, right.Id,
                        new XYZ(freeX - right.GetPlane().Origin.X, 0, 0));
                }
            }

            // Ajustar el plano superior (Top) siempre
            if (planes.TryGetValue("Top", out var top))
                ElementTransformUtils.MoveElement(doc, top.Id,
                    new XYZ(0, 0, heightFt - top.GetPlane().Origin.Z));
        }

        private static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "SinNombre";
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string invalidRegStr = string.Format(@"([{0}]|\.+$)", invalidChars);
            return Regex.Replace(s, invalidRegStr, "_").Trim();
        }

        private class WarningSwallower : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor fa)
            {
                foreach (FailureMessageAccessor f in fa.GetFailureMessages())
                    if (f.GetSeverity() == FailureSeverity.Warning)
                        fa.DeleteWarning(f);
                return FailureProcessingResult.Continue;
            }
        }
    }
}
