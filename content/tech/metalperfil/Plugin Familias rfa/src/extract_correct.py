import ezdxf
import json
import math
import re
import sys
from collections import defaultdict

sys.stdout.reconfigure(encoding='utf-8')


def parse_mtext(raw):
    """Return first line of MTEXT after stripping formatting codes."""
    # Split on \P (paragraph break) BEFORE any cleanup — otherwise
    # codes like \P\H0.625x; get consumed by the generic code remover.
    # Use uppercase \P only: lowercase \p is used by tab stops (\pt480;).
    parts = re.split(r'\\P', raw)
    first = parts[0]
    # Now clean formatting codes from the first part only
    first = re.sub(r'\\f[^;]*;', '', first)            # \f@font|...;
    first = re.sub(r'\\[a-zA-Z0-9.][^;]*;', '', first) # \W...; \H...; etc.
    first = re.sub(r'\\[a-zA-Z]', '', first)            # bare codes \n, \t
    first = re.sub(r'[{}]', '', first)
    return first.strip()


def parse_label(text):
    """
    Split 'FamilyName TypeString' from a cleaned label.
    Returns (family, type_str) or (None, None).
    """
    m = re.match(r'^([A-Za-z]+)\s+(.+)$', text)
    if not m:
        return None, None
    family = m.group(1)
    type_str = m.group(2).strip()
    # Normalise all Kaotico/kaotiko variants to Kaotiko
    if family.lower() == 'kaotico' or family.lower() == 'kaotiko':
        family = 'Kaotiko'
    return family, type_str


def poly_dedup_key(pts, length_bucket=10, pos_bucket=50):
    """
    Key for deduplicating near-identical polylines at the same position.
    Two polylines that share the same key are treated as the same drawing.
    """
    length = sum(math.dist(pts[i][:2], pts[i + 1][:2]) for i in range(len(pts) - 1))
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return (
        len(pts),
        round(length / length_bucket),
        round(min(xs) / pos_bucket),
        round(min(ys) / pos_bucket),
    )


def extract():
    doc = ezdxf.readfile("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf")
    msp = doc.modelspace()

    # ── 1. Labels from TITULOS layer ─────────────────────────────────────────
    labels = []
    for e in msp.query("TEXT MTEXT"):
        if e.dxf.layer != "TITULOS":
            continue
        raw = e.dxf.text if e.dxftype() == "TEXT" else e.text
        line = parse_mtext(raw)
        family, type_str = parse_label(line)
        if not family:
            continue
        labels.append({
            'family': family,
            'type': type_str,
            'pos': (e.dxf.insert.x, e.dxf.insert.y),
            'text': line,
        })

    print(f"Labels found (TITULOS): {len(labels)}")

    # ── 2. Polylines from FIBRA NEUTRA only ───────────────────────────────────
    raw_polys = []
    for e in msp.query("LWPOLYLINE POLYLINE"):
        if e.dxf.layer != "FIBRA NEUTRA":
            continue
        if e.dxftype() == "LWPOLYLINE":
            pts = [(p[0], p[1], p[4]) for p in e.get_points()]
        else:
            pts = []
            for v in e.vertices:
                b = v.dxf.bulge if v.dxf.hasattr('bulge') else 0.0
                pts.append((v.dxf.location.x, v.dxf.location.y, b))
        if len(pts) < 3:
            continue
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        raw_polys.append({
            'pts': pts,
            'min_x': min(xs),
            'center_y': (min(ys) + max(ys)) / 2,
        })

    print(f"Raw FIBRA NEUTRA polylines: {len(raw_polys)}")

    # ── 3. Deduplicate polylines at the same position ────────────────────────
    seen_keys = {}
    polys = []
    for p in raw_polys:
        key = poly_dedup_key(p['pts'])
        if key not in seen_keys:
            seen_keys[key] = True
            polys.append(p)

    print(f"After deduplication: {len(polys)} polylines")

    # ── 4. Build candidate (dist, label, poly) pairs ─────────────────────────
    # The label must be to the LEFT of the polyline's leftmost vertex.
    # dx = poly.min_x - label.x  (positive → label is left of poly)
    # Limits: horizontal gap 0–2000 mm; combined distance < 3000 mm.
    MAX_DX   = 2000
    MAX_DIST = 3000

    candidates = []
    for lbl in labels:
        lx, ly = lbl['pos']
        for p in polys:
            dx = p['min_x'] - lx
            if dx < -300 or dx > MAX_DX:
                continue
            dy = abs(p['center_y'] - ly)
            dist = math.sqrt(dx ** 2 + (dy * 2) ** 2)
            if dist > MAX_DIST:
                continue
            candidates.append((dist, lbl, p))

    candidates.sort(key=lambda x: x[0])

    # ── 5. Greedy exclusive assignment ────────────────────────────────────────
    used_labels = set()
    used_polys = set()
    matches = []

    for dist, lbl, p in candidates:
        if id(lbl) in used_labels or id(p) in used_polys:
            continue
        used_labels.add(id(lbl))
        used_polys.add(id(p))
        matches.append((lbl, p, dist))

    # ── 6. Report unmatched ───────────────────────────────────────────────────
    unmatched_labels = [l for l in labels if id(l) not in used_labels]
    unmatched_polys  = [p for p in polys  if id(p) not in used_polys]

    print(f"\nMatched: {len(matches)}")
    if unmatched_labels:
        print(f"Unmatched labels ({len(unmatched_labels)}):")
        for l in unmatched_labels:
            print(f"  '{l['text']}' at ({l['pos'][0]:.0f}, {l['pos'][1]:.0f})")
    if unmatched_polys:
        length = lambda p: sum(math.dist(p['pts'][i][:2], p['pts'][i+1][:2]) for i in range(len(p['pts'])-1))
        print(f"Unmatched polys ({len(unmatched_polys)}):")
        for p in unmatched_polys:
            print(f"  {len(p['pts'])} verts, center_y={p['center_y']:.0f}, min_x={p['min_x']:.0f}, len={length(p):.0f}")

    # ── 7. Build JSON output grouped by family ────────────────────────────────
    families = defaultdict(list)
    for lbl, p, _ in matches:
        families[lbl['family']].append({
            'type': lbl['type'],
            'vertices': [[round(v[0], 3), round(v[1], 3), round(v[2], 5)] for v in p['pts']],
        })

    for fam in families:
        families[fam].sort(key=lambda x: x['type'])

    output = dict(sorted(families.items()))

    print(f"\nFamilies in output ({len(output)}):")
    for fam, types in output.items():
        print(f"  {fam}: {[t['type'] for t in types]}")

    with open("perfiles_extraidos.json", "w", encoding="utf-8") as f:
        json.dump(output, f, indent=2, ensure_ascii=False)
    with open("perfiles_test.json", "w", encoding="utf-8") as f:
        json.dump(output, f, indent=2, ensure_ascii=False)

    print("\nSaved: perfiles_extraidos.json and perfiles_test.json")


if __name__ == "__main__":
    extract()
