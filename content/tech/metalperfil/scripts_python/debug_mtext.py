import ezdxf, sys, re
sys.stdout.reconfigure(encoding='utf-8')
doc = ezdxf.readfile("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf")
msp = doc.modelspace()

def parse_mtext(raw):
    parts = re.split(r'\\[pP]', raw)
    first = parts[0]
    first = re.sub(r'\\f[^;]*;', '', first)
    first = re.sub(r'\\[a-zA-Z0-9.][^;]*;', '', first)
    first = re.sub(r'\\[a-zA-Z]', '', first)
    first = re.sub(r'[{}]', '', first)
    return first.strip()

print("All TITULOS labels and parse result:")
for e in msp.query("TEXT MTEXT"):
    if e.dxf.layer != "TITULOS": continue
    raw = e.dxf.text if e.dxftype() == "TEXT" else e.text
    line = parse_mtext(raw)
    m = re.match(r'^([A-Za-z]+)\s+(.+)$', line)
    status = "OK" if m else "FAIL"
    print(f"  [{status}] raw={repr(raw[:80])}")
    print(f"         -> first_line={repr(line)}")
