# -*- coding: utf-8 -*-
"""
convertir_png_pesados.py  —  PNG fotograficos pesados -> JPEG (MSTos)

Los renders guardados como PNG pesan 5-15 MB y no ganan nada frente a un JPEG
de calidad alta. Este script convierte SOLO los PNG que:
  - estan referenciados por index.html,
  - pesan mas de UMBRAL_MB,
  - son opacos (sin transparencia real),
  - y no son logos (se excluyen por nombre).
Escribe el .jpg al lado, deja el .png original intacto y reescribe la referencia
en index.html.

USO:  python convertir_png_pesados.py --dry
      python convertir_png_pesados.py
"""
import io
import os
import re
import sys
from PIL import Image

try:
    from urllib.parse import unquote
except ImportError:  # py2
    from urllib import unquote

Image.MAX_IMAGE_PIXELS = None

BASE = os.path.dirname(os.path.abspath(__file__))
IDX = os.path.join(BASE, "index.html")
UMBRAL_MB = 1.5
QUALITY = 88
EXCLUIR = ("logo", "favicon")
DRY = "--dry" in sys.argv


def opaco(im):
    """Opaca de verdad, o con una franja transparente residual del render (<0,5%)."""
    if im.mode not in ("RGBA", "LA", "P"):
        return True
    if im.mode == "P" and "transparency" not in im.info:
        return True
    alpha = im.convert("RGBA").getchannel("A")
    if alpha.getextrema()[0] == 255:
        return True
    hist = alpha.histogram()
    return (sum(hist[:250]) / float(sum(hist))) < 0.005


with io.open(IDX, encoding="utf-8") as f:
    html = f.read()

refs = sorted(set(
    m.group(1) for m in re.finditer(r'(?:src|href|poster)="((?:content|assets)/[^"]+\.png)"', html)
))

total_antes = total_despues = 0
cambios = []
for ref in refs:
    rel = unquote(ref).replace("/", os.sep)
    src = os.path.join(BASE, rel)
    if not os.path.exists(src):
        print("  !! no existe %s" % ref)
        continue
    name = os.path.basename(src).lower()
    if any(x in name for x in EXCLUIR):
        continue
    mb = os.path.getsize(src) / 1e6
    if mb < UMBRAL_MB:
        continue
    im = Image.open(src)
    if not opaco(im):
        print("  -- %s tiene transparencia, se deja en PNG" % ref)
        continue

    dst = os.path.splitext(src)[0] + ".jpg"
    if not DRY:
        plano = Image.new("RGB", im.size, (255, 255, 255))
        rgba = im.convert("RGBA")
        plano.paste(rgba, mask=rgba.getchannel("A"))
        plano.save(dst, "JPEG", quality=QUALITY, optimize=True, progressive=True)
    nueva_mb = (os.path.getsize(dst) / 1e6) if os.path.exists(dst) else 0
    total_antes += mb
    total_despues += nueva_mb
    cambios.append((ref, os.path.splitext(ref)[0] + ".jpg"))
    print("  %-62s %6.2f MB -> %6.2f MB" % (os.path.basename(src), mb, nueva_mb))

if not DRY:
    for viejo, nuevo in cambios:
        html = html.replace('"%s"' % viejo, '"%s"' % nuevo)
    with io.open(IDX, "w", encoding="utf-8", newline="") as f:
        f.write(html)

print("-" * 62)
print("%d imagenes  |  %.1f MB -> %.1f MB  (%s)" % (
    len(cambios), total_antes, total_despues, "SECO" if DRY else "aplicado"))
