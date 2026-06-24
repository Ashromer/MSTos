import json
import math
import re
from collections import Counter

with open("perfiles_final.json", "r", encoding="utf-8") as f:
    data = json.load(f)

print(f"Total familias: {len(data)}\n")

print("=== ESTRUCTURA POR FAMILIA ===")
for family, types in data.items():
    type_names = [t["type"] for t in types]
    print(f"  {family}: {len(types)} tipos -> {type_names}")

print()
print("=== PROBLEMAS DETECTADOS ===")

# 1. Tipos duplicados dentro de una familia
dupes_found = False
for family, types in data.items():
    type_counts = Counter(t["type"] for t in types)
    for typ, count in type_counts.items():
        if count > 1:
            print(f"[!] Duplicado: {family} '{typ}' aparece {count} veces")
            dupes_found = True
if not dupes_found:
    print("[OK] Sin tipos duplicados")

# 2. Nombres sucios (caracteres no esperados)
dirty_found = False
for family, types in data.items():
    if re.search(r'[{}\\]', family):
        print(f"[!] Familia con nombre sucio: {family!r}")
        dirty_found = True
    for t in types:
        if re.search(r'[{}\\]', t["type"]):
            print(f"[!] Tipo sucio en {family}: {t['type']!r}")
            dirty_found = True
if not dirty_found:
    print("[OK] Sin caracteres sucios en nombres")

# 3. Polilíneas con muy pocos vértices
few_found = False
for family, types in data.items():
    for t in types:
        nv = len(t["vertices"])
        if nv < 3:
            print(f"[!] Pocos vértices: {family} '{t['type']}' tiene {nv} vértices")
            few_found = True
if not few_found:
    print("[OK] Todos los perfiles tienen >= 3 vértices")

print()
print("=== DIMENSIONES DE CADA PERFIL (mm) ===")
print(f"{'Familia':<18} {'Tipo':<20} {'DX':>8} {'DY':>8} {'Length':>10} {'Verts':>6}")
rows = []
for family, types in data.items():
    for t in types:
        pts = t["vertices"]
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        dx = max(xs) - min(xs)
        dy = max(ys) - min(ys)
        length = sum(math.dist(pts[i], pts[i+1]) for i in range(len(pts)-1))
        rows.append((family, t["type"], dx, dy, length, len(pts)))

for family, typ, dx, dy, length, nv in sorted(rows, key=lambda x: (x[0], x[1])):
    print(f"{family:<18} {typ:<20} {dx:>8.1f} {dy:>8.1f} {length:>10.1f} {nv:>6}")
