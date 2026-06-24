using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using netDxf;
using netDxf.Entities;
using ACadSharp;
using ACadSharp.IO;

namespace PluginFamilias.CAD
{
    public class PolylineData
    {
        public List<PolylineVertex> Vertices { get; set; } = new List<PolylineVertex>();
        public double MinX { get; set; }
        public double CenterY { get; set; }
        public string FamilyName { get; set; } = "Generico";
        public string TypeName { get; set; } = "Tipo_Desconocido";
        public string Name => $"{FamilyName} {TypeName}";
        public bool IsSelected { get; set; } = true;
    }

    public class PolylineVertex
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Bulge { get; set; }
    }

    public class CadExtractor
    {
        private struct Label
        {
            public string Family;
            public string Type;
            public double X;
            public double Y;
            public string RawText;
        }

        public List<PolylineData> Extract(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            
            if (ext == ".dxf")
                return ExtractDxf(filePath);
            else if (ext == ".dwg")
                return ExtractDwg(filePath);
            else
                throw new Exception("Formato no soportado.");
        }

        private List<PolylineData> ExtractDxf(string path)
        {
            var results = new List<PolylineData>();
            DxfDocument doc = DxfDocument.Load(path);
            if (doc == null) return results;

            var labelList = new List<Label>();
            foreach (var text in doc.Entities.Texts.Where(t => t.Layer.Name.Equals("TITULOS", StringComparison.OrdinalIgnoreCase)))
            {
                var parsed = ParseLabel(text.Value, text.Position.X, text.Position.Y);
                if (parsed.HasValue) labelList.Add(parsed.Value);
            }
            foreach (var mtext in doc.Entities.MTexts.Where(t => t.Layer.Name.Equals("TITULOS", StringComparison.OrdinalIgnoreCase)))
            {
                var parsed = ParseLabel(CleanMText(mtext.Value), mtext.Position.X, mtext.Position.Y);
                if (parsed.HasValue) labelList.Add(parsed.Value);
            }

            var rawPolys = new List<PolylineData>();
            foreach (var poly in doc.Entities.Polylines2D.Where(p => p.Layer.Name.Equals("FIBRA NEUTRA", StringComparison.OrdinalIgnoreCase)))
            {
                var data = new PolylineData();
                foreach (var v in poly.Vertexes)
                {
                    data.Vertices.Add(new PolylineVertex { X = v.Position.X, Y = v.Position.Y, Bulge = v.Bulge });
                }
                if (data.Vertices.Count < 3) continue;
                rawPolys.Add(ProcessData(data));
            }

            var dedupedPolys = Deduplicate(rawPolys);
            return MatchLabelsToPolys(labelList, dedupedPolys);
        }

        private List<PolylineData> ExtractDwg(string path)
        {
            var results = new List<PolylineData>();
            using (DwgReader reader = new DwgReader(path))
            {
                CadDocument doc = reader.Read();
                
                var labelList = new List<Label>();
                var textEntities = doc.Entities.OfType<ACadSharp.Entities.TextEntity>()
                                    .Where(t => t.Layer.Name.Equals("TITULOS", StringComparison.OrdinalIgnoreCase));
                
                foreach (var t in textEntities)
                {
                    var parsed = ParseLabel(t.Value, t.InsertPoint.X, t.InsertPoint.Y);
                    if (parsed.HasValue) labelList.Add(parsed.Value);
                }

                var mtextEntities = doc.Entities.OfType<ACadSharp.Entities.MText>()
                                     .Where(t => t.Layer.Name.Equals("TITULOS", StringComparison.OrdinalIgnoreCase));

                foreach (var mt in mtextEntities)
                {
                    var parsed = ParseLabel(CleanMText(mt.Value), mt.InsertPoint.X, mt.InsertPoint.Y);
                    if (parsed.HasValue) labelList.Add(parsed.Value);
                }

                var polylines = doc.Entities.OfType<ACadSharp.Entities.LwPolyline>()
                                   .Where(p => p.Layer.Name.Equals("FIBRA NEUTRA", StringComparison.OrdinalIgnoreCase));

                var rawPolys = new List<PolylineData>();
                foreach (var poly in polylines)
                {
                    var data = new PolylineData();
                    foreach (var v in poly.Vertices)
                    {
                        data.Vertices.Add(new PolylineVertex { X = v.Location.X, Y = v.Location.Y, Bulge = v.Bulge });
                    }
                    if (data.Vertices.Count < 3) continue;
                    rawPolys.Add(ProcessData(data));
                }

                var dedupedPolys = Deduplicate(rawPolys);
                return MatchLabelsToPolys(labelList, dedupedPolys);
            }
        }

        private Label? ParseLabel(string text, double x, double y)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            
            // Regex: Start with alpha (Family), followed by space and rest (Type)
            var match = Regex.Match(text, @"^([A-Za-z]+)\s+(.+)$");
            if (!match.Success) return null;

            string family = match.Groups[1].Value;
            string type = match.Groups[2].Value.Trim();

            if (family.Equals("kaotico", StringComparison.OrdinalIgnoreCase) || 
                family.Equals("kaotiko", StringComparison.OrdinalIgnoreCase))
                family = "Kaotiko";

            return new Label { Family = family, Type = type, X = x, Y = y, RawText = text };
        }

        private string CleanMText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            // Split on \P (paragraph break) and take first part
            string first = raw.Split(new[] { "\\P" }, StringSplitOptions.None)[0];
            // Remove formatting codes like \f...; \W...; \H...;
            first = Regex.Replace(first, @"\\f[^;]*;", "");
            first = Regex.Replace(first, @"\\[a-zA-Z0-9.][^;]*;", "");
            first = Regex.Replace(first, @"\\[a-zA-Z]", "");
            first = Regex.Replace(first, @"[{}]", "");
            return first.Trim();
        }

        private List<PolylineData> Deduplicate(List<PolylineData> polys)
        {
            var deduped = new List<PolylineData>();
            var seenKeys = new HashSet<string>();

            foreach (var p in polys)
            {
                double length = 0;
                for (int i = 0; i < p.Vertices.Count - 1; i++)
                {
                    length += Math.Sqrt(Math.Pow(p.Vertices[i+1].X - p.Vertices[i].X, 2) + Math.Pow(p.Vertices[i+1].Y - p.Vertices[i].Y, 2));
                }

                // Key: (VertexCount, LengthBucket, MinXBucket, MinYBucket)
                string key = $"{p.Vertices.Count}|{Math.Round(length / 10.0)}|{Math.Round(p.MinX / 50.0)}|{Math.Round(p.Vertices.Select(v => v.Y).Min() / 50.0)}";
                
                if (seenKeys.Add(key))
                {
                    deduped.Add(p);
                }
            }
            return deduped;
        }

        private List<PolylineData> MatchLabelsToPolys(List<Label> labels, List<PolylineData> polys)
        {
            var candidates = new List<(double dist, Label lbl, PolylineData poly)>();
            const double MAX_DX = 2000;
            const double MAX_DIST = 3000;

            foreach (var lbl in labels)
            {
                foreach (var p in polys)
                {
                    double dx = p.MinX - lbl.X;
                    if (dx < -300 || dx > MAX_DX) continue;

                    double dy = Math.Abs(p.CenterY - lbl.Y);
                    double dist = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy * 2, 2));
                    
                    if (dist < MAX_DIST)
                    {
                        candidates.Add((dist, lbl, p));
                    }
                }
            }

            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            var usedLabels = new HashSet<Label>();
            var usedPolys = new HashSet<PolylineData>();
            var finalResults = new List<PolylineData>();

            foreach (var (dist, lbl, poly) in candidates)
            {
                if (usedLabels.Contains(lbl) || usedPolys.Contains(poly)) continue;

                usedLabels.Add(lbl);
                usedPolys.Add(poly);

                poly.FamilyName = lbl.Family;
                poly.TypeName = lbl.Type;
                finalResults.Add(poly);
            }

            return finalResults.OrderBy(r => r.Name).ToList();
        }

        private PolylineData ProcessData(PolylineData data)
        {
            if (data.Vertices.Count > 0)
            {
                var xs = data.Vertices.Select(v => v.X);
                var ys = data.Vertices.Select(v => v.Y);
                data.MinX = xs.Min();
                data.CenterY = (ys.Min() + ys.Max()) / 2.0;
            }
            return data;
        }
    }
}
