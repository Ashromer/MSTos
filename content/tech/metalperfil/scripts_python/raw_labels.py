import ezdxf
import sys

doc = ezdxf.readfile("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf")
msp = doc.modelspace()

sys.stdout.reconfigure(encoding='utf-8')

print("=== RAW ENCABEZADOS ===")
for i, e in enumerate(msp.query("TEXT MTEXT")):
    if e.dxf.layer != "ENCABEZADOS":
        continue
    etype = e.dxftype()
    try:
        raw = e.dxf.text if etype == "TEXT" else e.text
    except Exception as ex:
        raw = f"<ERROR: {ex}>"
    try:
        pos = (e.dxf.insert.x, e.dxf.insert.y)
    except Exception as ex:
        pos = (-1, -1)
    print(f"[{i}] {etype} at {pos[0]:.0f},{pos[1]:.0f} | raw={repr(raw[:80])}")
