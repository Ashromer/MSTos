import ezdxf
import re
import math

doc = ezdxf.readfile("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf")
msp = doc.modelspace()

print("=== ENCABEZADOS TEXTS ===")
for e in msp.query("TEXT MTEXT"):
    if e.dxf.layer == "ENCABEZADOS":
        txt = e.dxf.text if e.dxftype() == "TEXT" else e.text
        txt_clean = re.sub(r"\\[a-zA-Z0-9|.]+;", "", txt)
        txt_clean = re.sub(r"[{}]", "", txt_clean).strip()
        pos = e.dxf.insert
        print(f"  [{e.dxftype()}] {repr(txt_clean[:60])} at ({pos.x:.1f}, {pos.y:.1f})")

print()
print("=== FIBRA NEUTRA POLYLINES ===")
fibra_ents = [e for e in msp.query("LWPOLYLINE POLYLINE") if e.dxf.layer == "FIBRA NEUTRA"]
for i, e in enumerate(fibra_ents):
    if e.dxftype() == "LWPOLYLINE":
        pts = [(p[0], p[1]) for p in e.get_points()]
    else:
        pts = [(p.dxf.location.x, p.dxf.location.y) for p in e.points()]
    length = sum(math.dist(pts[j], pts[j+1]) for j in range(len(pts)-1))
    min_x = min(p[0] for p in pts)
    min_y = min(p[1] for p in pts)
    max_y = max(p[1] for p in pts)
    print(f"  [{i}] verts={len(pts)}, len={length:.0f}, min_x={min_x:.1f}, y_range=({min_y:.1f},{max_y:.1f})")

print()
print("=== RECUADROS POLYLINES (sample 5) ===")
for i, e in enumerate(msp.query('LWPOLYLINE[layer=="RECUADROS"]')):
    if i >= 5:
        break
    pts = [(p[0], p[1]) for p in e.get_points()]
    min_x = min(p[0] for p in pts)
    max_x = max(p[0] for p in pts)
    min_y = min(p[1] for p in pts)
    max_y = max(p[1] for p in pts)
    print(f"  [{i}] verts={len(pts)}, bbox=({min_x:.1f},{min_y:.1f}) -> ({max_x:.1f},{max_y:.1f})")

print()
print("=== ANOTACIONES TEXTS (sample 20) ===")
count = 0
for e in msp.query("TEXT MTEXT"):
    if e.dxf.layer == "ANOTACIONES":
        txt = e.dxf.text if e.dxftype() == "TEXT" else e.text
        txt_clean = re.sub(r"\\[a-zA-Z0-9|.]+;", "", txt)
        txt_clean = re.sub(r"[{}]", "", txt_clean).strip()
        pos = e.dxf.insert
        print(f"  [{e.dxftype()}] {repr(txt_clean[:60])} at ({pos.x:.1f}, {pos.y:.1f})")
        count += 1
        if count >= 20:
            break
