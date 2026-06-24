import ezdxf
import re
import math

doc = ezdxf.readfile("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf")
msp = doc.modelspace()

def clean_mtext(raw):
    # Remove font/formatting codes like \f@...|...; and \f...; etc.
    text = re.sub(r'\\f[^;]*;', '', raw)
    text = re.sub(r'\\[a-zA-Z][^;]*;', '', text)
    text = re.sub(r'\\[a-zA-Z]', '', text)
    text = re.sub(r'[{}]', '', text)
    return text.strip()

# ── 1. ALL LABEL TEXTS (ENCABEZADOS) ─────────────────────────────────────────
print("=== ENCABEZADOS — todas las líneas ===")
enc_labels = []
for e in msp.query("TEXT MTEXT"):
    if e.dxf.layer != "ENCABEZADOS":
        continue
    raw = e.dxf.text if e.dxftype() == "TEXT" else e.text
    clean = clean_mtext(raw)
    pos = (e.dxf.insert.x, e.dxf.insert.y)
    # Split MTEXT paragraph breaks → take only first line
    lines = [l.strip() for l in re.split(r'\\[pP]', clean) if l.strip()]
    first_line = lines[0] if lines else ""
    has_number = bool(re.search(r'\d', first_line))
    marker = "  [LABEL]" if has_number else "  [HEADER]"
    print(f"{marker} {repr(first_line):<35} at ({pos[0]:.0f}, {pos[1]:.0f})")
    if has_number:
        enc_labels.append({"text": first_line, "pos": pos, "lines": lines})

print(f"\nTotal labels con número: {len(enc_labels)}")

# ── 2. ANOTACIONES TEXTS ──────────────────────────────────────────────────────
print("\n=== ANOTACIONES (primeras 40) ===")
anot_labels = []
count = 0
for e in msp.query("TEXT MTEXT"):
    if e.dxf.layer != "ANOTACIONES":
        continue
    raw = e.dxf.text if e.dxftype() == "TEXT" else e.text
    clean = clean_mtext(raw)
    lines = [l.strip() for l in re.split(r'\\[pP]', clean) if l.strip()]
    first_line = lines[0] if lines else ""
    pos = (e.dxf.insert.x, e.dxf.insert.y)
    # Only show profile-like labels
    if re.match(r'^[A-Za-z].{1,20}\d', first_line):
        print(f"  {repr(first_line):<35} at ({pos[0]:.0f}, {pos[1]:.0f})")
        anot_labels.append({"text": first_line, "pos": pos})
        count += 1
        if count >= 40:
            break

# ── 3. FIBRA NEUTRA polylines ─────────────────────────────────────────────────
print("\n=== FIBRA NEUTRA — todas las polilíneas ===")
print(f"{'#':<4} {'verts':>6} {'DX':>8} {'DY':>8} {'length':>10}  bbox_x          bbox_y")
fibra = []
for e in msp.query("LWPOLYLINE POLYLINE"):
    if e.dxf.layer != "FIBRA NEUTRA":
        continue
    if e.dxftype() == "LWPOLYLINE":
        pts = [(p[0], p[1]) for p in e.get_points()]
    else:
        pts = [(p.dxf.location.x, p.dxf.location.y) for p in e.points()]
    if len(pts) < 2:
        continue
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    dx = max(xs) - min(xs)
    dy = max(ys) - min(ys)
    length = sum(math.dist(pts[i], pts[i+1]) for i in range(len(pts)-1))
    fibra.append({"pts": pts, "dx": dx, "dy": dy, "length": length,
                  "min_x": min(xs), "max_x": max(xs), "min_y": min(ys), "max_y": max(ys),
                  "center_y": (min(ys)+max(ys))/2})

fibra.sort(key=lambda f: f["center_y"], reverse=True)
for i, f in enumerate(fibra):
    print(f"{i:<4} {len(f['pts']):>6} {f['dx']:>8.1f} {f['dy']:>8.1f} {f['length']:>10.1f}"
          f"  x[{f['min_x']:.0f}..{f['max_x']:.0f}]  y[{f['min_y']:.0f}..{f['max_y']:.0f}]")

# ── 4. PERFIL ENCABEZADO polylines ────────────────────────────────────────────
print("\n=== PERFIL ENCABEZADO — polilíneas ===")
for e in msp.query("LWPOLYLINE POLYLINE"):
    if e.dxf.layer != "PERFIL ENCABEZADO":
        continue
    if e.dxftype() == "LWPOLYLINE":
        pts = [(p[0], p[1]) for p in e.get_points()]
    else:
        pts = [(p.dxf.location.x, p.dxf.location.y) for p in e.points()]
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    dx = max(xs) - min(xs)
    dy = max(ys) - min(ys)
    length = sum(math.dist(pts[i], pts[i+1]) for i in range(len(pts)-1))
    print(f"  verts={len(pts)}, dx={dx:.1f}, dy={dy:.1f}, len={length:.1f}"
          f"  x[{min(xs):.0f}..{max(xs):.0f}]  y[{min(ys):.0f}..{max(ys):.0f}]")

# ── 5. Match ENCABEZADOS labels → FIBRA NEUTRA ───────────────────────────────
print("\n=== MATCHING: label → polilínea más cercana ===")
for lbl in enc_labels:
    lx, ly = lbl["pos"]
    candidates = []
    for f in fibra:
        # Label should be to the left of polyline's min_x
        dx = f["min_x"] - lx
        dy = abs(f["center_y"] - ly)
        if dx > -300:  # label left of or near polyline
            candidates.append((math.sqrt(dx**2 + (dy*2)**2), f, dx, dy))
    candidates.sort(key=lambda x: x[0])
    best = candidates[0] if candidates else None
    if best:
        dist, f, dx, dy = best
        print(f"  {lbl['text']:<30} → dy_profile={f['dy']:.1f}  verts={len(f['pts'])}  "
              f"  dx_to_label={dx:.0f}  dy_to_label={dy:.0f}  dist={dist:.0f}")
    else:
        print(f"  {lbl['text']:<30} → NO MATCH")
