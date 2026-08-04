# -*- coding: utf-8 -*-
"""
extraer_paginas_pdf.py  —  Genera las imagenes de los "carretes" de planos (MSTos)

Rasteriza las paginas de los PDF de proyecto a JPG listos para web (2200 px, q82)
y recomprime las paginas de Waraqa que ya venian exportadas como PNG gigantes.

Re-ejecutable: si el JPG destino ya existe y es mas nuevo que el origen, lo salta.

USO:  python extraer_paginas_pdf.py
      python extraer_paginas_pdf.py --force
"""
import os
import sys
from PIL import Image
import fitz  # PyMuPDF

Image.MAX_IMAGE_PIXELS = None

BASE = os.path.dirname(os.path.abspath(__file__))
MAX_DIM = 2200
QUALITY = 82
FORCE = "--force" in sys.argv


def _fresh(src, dst):
    return (not FORCE) and os.path.exists(dst) and os.path.getmtime(dst) >= os.path.getmtime(src)


def _save(img, dst):
    if img.mode != "RGB":
        img = img.convert("RGB")
    w, h = img.size
    if max(w, h) > MAX_DIM:
        s = MAX_DIM / float(max(w, h))
        img = img.resize((int(w * s), int(h * s)), Image.LANCZOS)
    img.save(dst, "JPEG", quality=QUALITY, optimize=True, progressive=True)
    print("   %-40s %5.2f MB" % (os.path.basename(dst), os.path.getsize(dst) / 1e6))


def from_pdf(pdf_rel, out_prefix):
    """Rasteriza cada pagina del PDF a <out_prefix>_NN.jpg junto al propio PDF."""
    pdf = os.path.join(BASE, pdf_rel)
    folder = os.path.dirname(pdf)
    doc = fitz.open(pdf)
    print("PDF %s  (%d paginas)" % (os.path.basename(pdf), doc.page_count))
    for i, page in enumerate(doc, start=1):
        dst = os.path.join(folder, "%s_%02d.jpg" % (out_prefix, i))
        if _fresh(pdf, dst):
            continue
        zoom = MAX_DIM / max(page.rect.width, page.rect.height)
        pix = page.get_pixmap(matrix=fitz.Matrix(zoom, zoom), alpha=False)
        _save(Image.frombytes("RGB", (pix.width, pix.height), pix.samples), dst)
    doc.close()


def from_pngs(folder_rel, src_pattern, out_prefix, count):
    """Recomprime paginas ya exportadas como PNG (Waraqa) a JPG de web."""
    folder = os.path.join(BASE, folder_rel)
    print("PNG %s  (%d paginas)" % (src_pattern, count))
    for i in range(1, count + 1):
        src = os.path.join(folder, src_pattern % i)
        dst = os.path.join(folder, "%s_%02d.jpg" % (out_prefix, i))
        if not os.path.exists(src):
            print("   !! falta %s" % os.path.basename(src))
            continue
        if _fresh(src, dst):
            continue
        _save(Image.open(src), dst)


if __name__ == "__main__":
    from_pdf(os.path.join("content", "arquitectura", "campillos", "Proyecto Campillos.pdf"), "plano")
    from_pdf(os.path.join("content", "arquitectura", "Casa Antequera", "250410_CasaMigueSara.pdf"), "plano")
    from_pngs(os.path.join("content", "arquitectura", "waraqa"),
              u"20211102_ARQ DEFINITIVA_Página_%d.png", "arq", 9)
    from_pngs(os.path.join("content", "arquitectura", "waraqa"),
              u"20211107_MADERA_Página_%d.png", "madera", 9)
    print("Hecho.")
