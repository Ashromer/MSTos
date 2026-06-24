import ezdxf
import sys
import re
from collections import Counter

sys.stdout.reconfigure(encoding='utf-8')

doc = ezdxf.readfile("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf")
msp = doc.modelspace()

# Count ALL entity types per layer in modelspace
all_ents = list(msp)
print(f"Total entities in modelspace: {len(all_ents)}")
layer_type = Counter((e.dxf.layer, e.dxftype()) for e in all_ents)
print("\nAll (layer, type) combinations:")
for (layer, etype), count in sorted(layer_type.items(), key=lambda x: -x[1]):
    print(f"  {layer!r:25} {etype:15} {count}")

# Specifically look at PERFILES OCULTOS texts
print("\n=== PERFILES OCULTOS texts ===")
for e in msp.query("TEXT MTEXT"):
    if e.dxf.layer != "PERFILES OCULTOS":
        continue
    raw = e.dxf.text if e.dxftype() == "TEXT" else e.text
    clean = re.sub(r'\\f[^;]*;|\\[a-zA-Z0-9][^;]*;', '', raw)
    clean = re.sub(r'\\[a-zA-Z]', ' ', clean)
    clean = re.sub(r'[{}]', '', clean).strip()
    pos = (e.dxf.insert.x, e.dxf.insert.y)
    print(f"  {clean[:50]!r} at ({pos[0]:.0f},{pos[1]:.0f})")

# Specifically look at RECUADROS texts
print("\n=== RECUADROS texts ===")
for e in msp.query("TEXT MTEXT"):
    if e.dxf.layer != "RECUADROS":
        continue
    raw = e.dxf.text if e.dxftype() == "TEXT" else e.text
    clean = re.sub(r'\\f[^;]*;|\\[a-zA-Z0-9][^;]*;', '', raw)
    clean = re.sub(r'\\[a-zA-Z]', ' ', clean)
    clean = re.sub(r'[{}]', '', clean).strip()
    pos = (e.dxf.insert.x, e.dxf.insert.y)
    print(f"  {clean[:60]!r} at ({pos[0]:.0f},{pos[1]:.0f})")
