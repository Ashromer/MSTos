import ezdxf
import sys
import re
import math

sys.stdout.reconfigure(encoding='utf-8')

doc = ezdxf.readfile("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf")
msp = doc.modelspace()

def first_line(raw):
    """Extract first meaningful line from MTEXT, stripping formatting codes."""
    text = re.sub(r'\\f[^;]*;', '', raw)
    text = re.sub(r'\\[a-zA-Z0-9][^;]*;', '', text)
    text = re.sub(r'[{}]', '', text).strip()
    parts = re.split(r'\\[pP]', text)
    return parts[0].strip()

print("=== TITULOS layer — all MTEXT ===")
titulos = []
for e in msp.query("TEXT MTEXT"):
    if e.dxf.layer != "TITULOS":
        continue
    raw = e.dxf.text if e.dxftype() == "TEXT" else e.text
    line = first_line(raw)
    pos = (e.dxf.insert.x, e.dxf.insert.y)
    print(f"  {line!r:<40} at ({pos[0]:.0f}, {pos[1]:.0f})")
    titulos.append({"text": line, "pos": pos, "raw": raw})

print(f"\nTotal: {len(titulos)}")

# Now match TITULOS labels to FIBRA NEUTRA polylines
print("\n=== FIBRA NEUTRA polylines ===")
fibra = []
for e in msp.query("LWPOLYLINE POLYLINE"):
    if e.dxf.layer != "FIBRA NEUTRA":
        continue
    if e.dxftype() == "LWPOLYLINE":
        pts = [(p[0], p[1]) for p in e.get_points()]
    else:
        pts = [(p.dxf.location.x, p.dxf.location.y) for p in e.points()]
    if len(pts) < 3:
        continue
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    fibra.append({
        "pts": pts,
        "min_x": min(xs), "max_x": max(xs),
        "min_y": min(ys), "max_y": max(ys),
        "center_y": (min(ys) + max(ys)) / 2,
        "dy": max(ys) - min(ys),
    })

print(f"Total FIBRA NEUTRA polylines: {len(fibra)}")

# Match each label to nearest polyline (label to the LEFT of min_x)
print("\n=== MATCHING: TITULOS label -> FIBRA NEUTRA polyline ===")
print(f"{'Label':<35} {'match_dy':>8} {'label_y':>8} {'delta_y':>8} {'dx_to_label':>12} {'verts':>6}")

matched = []
for lbl in titulos:
    lx, ly = lbl["pos"]
    best = None
    best_dist = float("inf")
    for f in fibra:
        # Label should be to the LEFT of polyline
        dx = f["min_x"] - lx   # positive = label is left of polyline
        dy = abs(f["center_y"] - ly)
        if dx < -500:           # label too far to the right — skip
            continue
        dist = math.sqrt(dx**2 + (dy * 2) ** 2)
        if dist < best_dist:
            best_dist = dist
            best = (f, dx, dy)
    if best:
        f, dx, dy = best
        print(f"  {lbl['text']:<35} {f['dy']:>8.1f} {ly:>8.0f} {dy:>8.0f} {dx:>12.0f} {len(f['pts']):>6}")
        matched.append({"label": lbl, "poly": f, "dist": best_dist})
    else:
        print(f"  {lbl['text']:<35} NO MATCH")
