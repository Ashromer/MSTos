import ezdxf
import sys
import re
from collections import Counter

sys.stdout.reconfigure(encoding='utf-8')

doc = ezdxf.readfile("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf")
msp = doc.modelspace()

inserts = list(msp.query("INSERT"))
print(f"Total INSERTs in modelspace: {len(inserts)}")

# Inventory all entity types and layers inside each block
print("\n=== Block contents summary ===")
all_block_layers = Counter()
label_candidates = []

for ins in inserts:
    blk_name = ins.dxf.name
    blk_insert = (ins.dxf.insert.x, ins.dxf.insert.y)
    try:
        block = doc.blocks[blk_name]
    except Exception:
        continue
    for e in block:
        all_block_layers[e.dxf.layer] += 1
        if e.dxftype() in ("TEXT", "MTEXT"):
            raw = e.dxf.text if e.dxftype() == "TEXT" else e.text
            label_candidates.append({
                "block": blk_name,
                "block_insert": blk_insert,
                "layer": e.dxf.layer,
                "type": e.dxftype(),
                "raw": raw,
            })

print("Layers inside blocks:")
for layer, count in sorted(all_block_layers.items(), key=lambda x: -x[1]):
    print(f"  {layer!r}: {count}")

print(f"\nTotal TEXT/MTEXT in blocks: {len(label_candidates)}")

# Show all TEXT/MTEXT from blocks
print("\n=== All TEXT/MTEXT inside blocks ===")
for lc in label_candidates:
    raw = lc["raw"]
    # Clean: remove MTEXT formatting
    clean = re.sub(r'\\f[^;]*;', '', raw)
    clean = re.sub(r'\\[a-zA-Z0-9][^;]*;', '', clean)
    clean = re.sub(r'\\[a-zA-Z]', ' ', clean)
    clean = re.sub(r'[{}]', '', clean).strip()
    # First line only
    first = re.split(r'\s*\\P\s*|\n', clean)[0].strip() if clean else ""
    # Also split by actual newline
    first = first.split('\\P')[0].strip()

    print(f"  [{lc['layer']}] {first!r:<40} at block_ins=({lc['block_insert'][0]:.0f},{lc['block_insert'][1]:.0f})")
