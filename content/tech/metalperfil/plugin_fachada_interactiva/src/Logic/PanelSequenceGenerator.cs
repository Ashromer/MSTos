using System;
using System.Collections.Generic;
using System.Linq;

namespace PluginFachadaInteractiva.Logic
{
    public class PanelSequenceGenerator
    {
        // Junta final máxima admisible (mm). Opción B: ~0 → se fuerza la
        // sustitución de piezas por tipos más anchos hasta cuadrar el muro,
        // aceptando una deriva controlada en los porcentajes. Las celdas se
        // colocan al ancho real del perfil, así que los paneles se tocan.
        public const double FillToleranceMm = 0.5;

        /// <summary>
        /// Genera una secuencia aleatoria de tipos de panel que cubre el ancho del muro.
        /// Reparte por número de piezas (método de cuotas): los porcentajes se respetan
        /// con la máxima precisión posible en enteros. El sobrante devuelto es siempre
        /// menor que el ancho mínimo y lo absorbe la última pieza al estirarse.
        /// </summary>
        public List<PanelTypeInfo> Generate(
            IList<PanelTypeInfo> types,
            double wallWidthMm,
            out double remainderMm,
            int? seed = null,
            double solapeMm = 0)
        {
            remainderMm = 0;

            // Mapeamos a anchos útiles reduciendo el solape
            var valid = types
                .Where(t => t.Percentage > 0 && t.WidthMm > solapeMm)
                .Select(t => new { Info = t, WidthMm = t.WidthMm - solapeMm, t.Percentage })
                .ToList();

            if (valid.Count == 0 || wallWidthMm <= 0)
                return [];

            var rng = seed.HasValue ? new Random(seed.Value) : new Random();
            double totalPct = valid.Sum(t => t.Percentage);
            double minWidth = valid.Min(t => t.WidthMm);

            var counts = new int[valid.Count];
            var result = new List<int>(); // Guardamos los índices del mapping
            double accumulated = 0;

            while (wallWidthMm - accumulated >= minWidth)
            {
                int next = -1;
                double bestDeficit = double.NegativeInfinity;
                for (int i = 0; i < valid.Count; i++)
                {
                    if (valid[i].WidthMm > wallWidthMm - accumulated) continue;
                    double deficit = valid[i].Percentage / totalPct * (result.Count + 1) - counts[i];
                    if (deficit > bestDeficit)
                    {
                        bestDeficit = deficit;
                        next = i;
                    }
                }
                if (next < 0) break;

                counts[next]++;
                result.Add(next);
                accumulated += valid[next].WidthMm;
            }

            double remainder = wallWidthMm - accumulated;
            int guard = result.Count * 8 + 32;
            while (result.Count > 0 && guard-- > 0 && remainder > FillToleranceMm)
            {
                int bestIdx = -1;
                int bestTypeIdx = -1;
                double bestInc = 0;
                for (int i = 0; i < result.Count; i++)
                {
                    for (int j = 0; j < valid.Count; j++)
                    {
                        double inc = valid[j].WidthMm - valid[result[i]].WidthMm;
                        if (inc > bestInc && inc <= remainder)
                        {
                            bestInc = inc;
                            bestIdx = i;
                            bestTypeIdx = j;
                        }
                    }
                }
                if (bestIdx < 0 || bestTypeIdx < 0) break;

                result[bestIdx] = bestTypeIdx;
                accumulated += bestInc;
                remainder -= bestInc;
            }

            // Fisher-Yates shuffle
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            // La pieza más ancha al final
            if (result.Count > 1)
            {
                int widest = 0;
                for (int i = 1; i < result.Count; i++)
                    if (valid[result[i]].WidthMm > valid[result[widest]].WidthMm) widest = i;
                (result[widest], result[^1]) = (result[^1], result[widest]);
            }

            remainderMm = wallWidthMm - accumulated;
            return result.Select(idx => valid[idx].Info).ToList();
        }
    }
}
