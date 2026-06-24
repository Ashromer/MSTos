from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_CENTER
from reportlab.platypus import (BaseDocTemplate, PageTemplate, Frame,
    Paragraph, Spacer, Table, TableStyle, HRFlowable, Image,
    NextPageTemplate, PageBreak)
from reportlab.lib import colors
 
BLACK  = colors.HexColor('#1A1A1A')
GRAY   = colors.HexColor('#4A4A4A')
LIGHT  = colors.HexColor('#F5F5F5')
LINE   = colors.HexColor('#CCCCCC')
WHITE  = colors.white
YELLOW = colors.HexColor('#FFF3CD')
LOGO_PATH = '/home/claude/logo_mst.jpg'
 
W, H = A4
LINE_W      = 2.4
HALF_LW     = LINE_W / 2
BORDER_PATH = 0.85 * cm
BORDER_INSET = BORDER_PATH + HALF_LW
LOGO_W = 1.8 * cm
LOGO_H = LOGO_W * 0.66
 
def draw_page(canvas, doc):
    canvas.saveState()
    canvas.setStrokeColor(BLACK)
    canvas.setLineWidth(LINE_W)
    canvas.rect(BORDER_INSET, BORDER_INSET,
                W - 2*BORDER_INSET, H - 2*BORDER_INSET, fill=0, stroke=1)
    if doc.page >= 2:
        logo_x = (W - LOGO_W) / 2
        logo_y = H - BORDER_INSET - LOGO_H - 0.25*cm
        canvas.drawImage(LOGO_PATH, logo_x, logo_y,
                         width=LOGO_W, height=LOGO_H,
                         preserveAspectRatio=True, mask='auto')
        canvas.setFont('Helvetica', 8)
        canvas.setFillColor(GRAY)
        canvas.drawCentredString(W/2, BORDER_INSET + 0.3*cm, str(doc.page))
    canvas.restoreState()
 
def make_doc(path):
    inner = BORDER_INSET + 0.8*cm
    top_extra = LOGO_H + 0.5*cm
    frame_p1 = Frame(inner, inner, W-2*inner, H-2*inner, id='p1')
    frame_p2 = Frame(inner, inner, W-2*inner, H-2*inner-top_extra, id='p2')
    doc = BaseDocTemplate(path, pagesize=A4, pageTemplates=[
        PageTemplate(id='First', frames=[frame_p1], onPage=draw_page),
        PageTemplate(id='Later', frames=[frame_p2], onPage=draw_page),
    ])
    return doc
 
def make_styles():
    n  = ParagraphStyle('n',  fontName='Helvetica',         fontSize=10, textColor=GRAY,  leading=15, spaceAfter=4)
    b  = ParagraphStyle('b',  fontName='Helvetica-Bold',    fontSize=10, textColor=BLACK, leading=15, spaceAfter=4)
    h1 = ParagraphStyle('h1', fontName='Helvetica-Bold',    fontSize=13, textColor=BLACK, leading=18, spaceBefore=16, spaceAfter=6)
    h2 = ParagraphStyle('h2', fontName='Helvetica-Bold',    fontSize=11, textColor=BLACK, leading=16, spaceBefore=12, spaceAfter=4)
    h3 = ParagraphStyle('h3', fontName='Helvetica-Bold',    fontSize=10, textColor=BLACK, leading=14, spaceBefore=8,  spaceAfter=3)
    tm = ParagraphStyle('tm', fontName='Helvetica-Bold',    fontSize=14, textColor=BLACK, leading=20, alignment=TA_CENTER, spaceBefore=20, spaceAfter=6)
    ts = ParagraphStyle('ts', fontName='Helvetica-Bold',    fontSize=11, textColor=BLACK, leading=16, alignment=TA_CENTER, spaceAfter=4)
    mt = ParagraphStyle('mt', fontName='Helvetica-Oblique', fontSize=10, textColor=GRAY,  leading=15, alignment=TA_CENTER)
    bl = ParagraphStyle('bl', fontName='Helvetica',         fontSize=10, textColor=GRAY,  leading=15, leftIndent=14, spaceAfter=3)
    return dict(n=n, b=b, h1=h1, h2=h2, h3=h3, tm=tm, ts=ts, mt=mt, bl=bl)
 
S = make_styles()
 
def HR():    return HRFlowable(width='100%', thickness=0.5, color=LINE, spaceAfter=8, spaceBefore=4)
def SP(h=6): return Spacer(1, h)
def B(t):    return Paragraph(f"• {t}", S['bl'])
 
def logo_cover(w=5*cm):
    usable = W - 2*(BORDER_INSET + 0.8*cm)
    img = Image(LOGO_PATH, width=w, height=w*0.66)
    tbl = Table([[img]], colWidths=[usable])
    tbl.setStyle(TableStyle([('ALIGN',(0,0),(0,0),'CENTER')]))
    return tbl
 
def fee_table(rows, total_label, total_value):
    data = [['Fase','Descripcion','Importe']] + rows + [['', total_label, total_value]]
    t = Table(data, colWidths=[2.5*cm, 10.5*cm, 3.5*cm])
    t.setStyle(TableStyle([
        ('FONTNAME',(0,0),(-1,0),'Helvetica-Bold'),
        ('FONTNAME',(0,1),(-1,-2),'Helvetica'),
        ('FONTNAME',(0,-1),(-1,-1),'Helvetica-Bold'),
        ('FONTSIZE',(0,0),(-1,-1),10), ('TEXTCOLOR',(0,0),(-1,-1),BLACK),
        ('ROWBACKGROUNDS',(0,1),(-1,-2),[WHITE,LIGHT]),
        ('BACKGROUND',(0,0),(-1,0),LIGHT), ('BACKGROUND',(0,-1),(-1,-1),LIGHT),
        ('LINEBELOW',(0,0),(-1,0),0.5,LINE), ('LINEABOVE',(0,-1),(-1,-1),0.5,LINE),
        ('TOPPADDING',(0,0),(-1,-1),6), ('BOTTOMPADDING',(0,0),(-1,-1),6),
        ('LEFTPADDING',(0,0),(-1,-1),8), ('RIGHTPADDING',(0,0),(-1,-1),8),
        ('ALIGN',(2,0),(2,-1),'RIGHT'), ('VALIGN',(0,0),(-1,-1),'MIDDLE'),
    ]))
    return t
 
def cal_table(rows, highlight_row=None):
    t = Table(rows, colWidths=[2*cm, 4*cm, 10.5*cm])
    style = [
        ('FONTNAME',(0,0),(-1,0),'Helvetica-Bold'), ('FONTNAME',(0,1),(-1,-1),'Helvetica'),
        ('FONTSIZE',(0,0),(-1,-1),9), ('TEXTCOLOR',(0,0),(-1,-1),BLACK),
        ('ROWBACKGROUNDS',(0,1),(-1,-1),[WHITE,LIGHT]), ('BACKGROUND',(0,0),(-1,0),LIGHT),
        ('LINEBELOW',(0,0),(-1,0),0.5,LINE),
        ('TOPPADDING',(0,0),(-1,-1),5), ('BOTTOMPADDING',(0,0),(-1,-1),5),
        ('LEFTPADDING',(0,0),(-1,-1),8), ('RIGHTPADDING',(0,0),(-1,-1),8),
        ('VALIGN',(0,0),(-1,-1),'MIDDLE'),
    ]
    if highlight_row:
        style.append(('BACKGROUND',(0,highlight_row),(-1,highlight_row),YELLOW))
    t.setStyle(TableStyle(style))
    return t
 
def limit_box(items):
    LIMIT = colors.HexColor('#F0F0F0')
    LIMIT_LINE = colors.HexColor('#AAAAAA')
    rows = [[Paragraph(f"• {item}", S['bl'])] for item in items]
    t = Table(rows, colWidths=[16.5*cm])
    t.setStyle(TableStyle([
        ('BACKGROUND',(0,0),(-1,-1),LIMIT),
        ('BOX',(0,0),(-1,-1),0.5,LIMIT_LINE),
        ('TOPPADDING',(0,0),(-1,-1),5), ('BOTTOMPADDING',(0,0),(-1,-1),5),
        ('LEFTPADDING',(0,0),(-1,-1),10), ('RIGHTPADDING',(0,0),(-1,-1),10),
    ]))
    return t
 
 
# ===============================================================
# PDF 1 — OFERTA HONORARIOS CATALOGO METALPERFIL
# ===============================================================
def pdf_oferta_catalogo(path):
    doc = make_doc(path)
    s = []
    s += [SP(20),
          Paragraph("Desarrollo de Catalogo Tecnico de Productos Metalperfil", S['tm']),
          SP(8), Paragraph("Oferta de honorarios", S['ts']), Paragraph("Version 1.0", S['ts']),
          SP(30), logo_cover(), SP(20),
          Paragraph("<i>Consultor: Miguel Suarez Torres - Arquitecto / BIM &amp; Automation Specialist</i>", S['mt']),
          Paragraph("<i>Malaga, Espana, 25 de Mayo de 2026</i>", S['mt']),
          SP(30), NextPageTemplate('Later'), PageBreak()]
 
    s += [Paragraph("INDICE", S['h2']), HR()]
    for i, t in enumerate(["Contexto y objeto del encargo","Alcance y entregables",
                            "Arquitectura del proyecto","Consolidacion economica",
                            "Calendario de ejecucion","Cierre"], 1):
        s.append(Paragraph(f"{i}.  {t}", S['n']))
    s.append(SP(20))
 
    s += [Paragraph("1. CONTEXTO Y OBJETO DEL ENCARGO", S['h1']), HR(),
          Paragraph("El presente documento formaliza la oferta de honorarios para el desarrollo integro del Catalogo Tecnico de Productos de Metalperfil, encargo que comprende desde la conceptualizacion editorial hasta la entrega del archivo final listo para imprenta y distribucion digital.", S['n']),
          SP(6),
          Paragraph("El proyecto abarca cuatro disciplinas tecnicas simultaneas: diseno editorial en InDesign, modelado parametrico avanzado de familias en Revit (.rfa), visualizacion arquitectonica de alta fidelidad en Unreal Engine 5 mediante pipeline Datasmith, y redaccion tecnica de contenido de producto.", S['n']),
          SP(16)]
 
    s += [Paragraph("2. ALCANCE Y ENTREGABLES", S['h1']), HR(),
          Paragraph("El encargo se estructura en cuatro bloques de trabajo:", S['n']), SP(6),
          Paragraph("2.1 Diseno editorial y maquetacion", S['h3'])]
    for t in ["Definicion de reticula, tipografia y sistema visual del catalogo.",
              "Diseno de portada, contraportada y estructura de fichas de producto.",
              "Maquetacion de aproximadamente 50 paginas en Adobe InDesign.",
              "Exportacion en PDF de imprenta (CMYK, sangrado, marcas de corte) y PDF digital (RGB)."]:
        s.append(B(t))
    s += [SP(8), Paragraph("2.2 Modelado parametrico Revit", S['h3'])]
    for t in ["Modelado de familias parametricas (.rfa) de paneles plegados, soluciones de esquina y paneles microperforados.",
              "Parametrizacion de dimensiones, materiales y carta RAL de produccion.",
              "Organizacion y entrega del ecosistema completo de familias para uso interno de Metalperfil."]:
        s.append(B(t))
    s += [SP(8), Paragraph("2.3 Visualizacion en Unreal Engine 5", S['h3'])]
    for t in ["Importacion y optimizacion geometrica de modelos via Datasmith.",
              "Configuracion de shaders metalicos realistas, rugosidades fisicas y mapeo de carta RAL.",
              "Renderizado de alta fidelidad para las imagenes del catalogo.",
              "Encuadres de camara y composicion arquitectonica de cada producto."]:
        s.append(B(t))
    s += [SP(8), Paragraph("2.4 Redaccion tecnica", S['h3'])]
    for t in ["Elaboracion de fichas tecnicas descriptivas para cada familia de producto.",
              "Redaccion de memorias descriptivas, pies de foto y especificaciones tecnicas.",
              "Revision e incorporacion de correcciones del departamento de marketing."]:
        s.append(B(t))
    s.append(SP(16))
 
    s += [Paragraph("3. ARQUITECTURA DEL PROYECTO", S['h1']), HR(),
          Paragraph("El proyecto se ejecuta en tres etapas naturales:", S['n']), SP(6),
          Paragraph("3.1 Fase de fundamentos (semanas 1-3)", S['h3']),
          Paragraph("Clasificacion de material tecnico, definicion del sistema visual, layout base en InDesign y modelado inicial de familias en Revit.", S['n']), SP(6),
          Paragraph("3.2 Fase de produccion (semanas 4-7)", S['h3']),
          Paragraph("Cierre del ecosistema de familias Revit, exportacion a Unreal Engine 5, renders de alta fidelidad e insercion en InDesign. Redaccion del contenido tecnico.", S['n']), SP(6),
          Paragraph("3.3 Fase de cierre y revision (semanas 8-10)", S['h3']),
          Paragraph("Envio del PDF preliminar (50 paginas) para revision interna en la semana 8. Procesamiento de correcciones y entrega del archivo final en la semana 10.", S['n']),
          SP(16)]
 
    s += [Paragraph("4. CONSOLIDACION ECONOMICA", S['h1']), HR(),
          fee_table([
              ['Fase I', 'Diseno editorial, sistema visual y maquetacion InDesign', '4.500 EUR'],
              ['Fase II','Modelado parametrico Revit — ecosistema de familias (.rfa)', '3.500 EUR'],
              ['Fase III','Visualizacion Unreal Engine 5 — renders de alta fidelidad', '3.000 EUR'],
              ['Fase IV','Redaccion tecnica, revisiones y entrega final de produccion', '2.000 EUR'],
          ], 'TOTAL PROYECTO - Version 1.0', '13.000 EUR + IVA'),
          SP(10),
          Paragraph("El importe total asciende a <b>13.000 EUR + IVA (21%)</b>, resultando una factura total de 15.730 EUR. Facturacion en una unica emision a la entrega del catalogo completo en la semana 10.", S['n']),
          SP(16)]
 
    s += [Paragraph("5. CALENDARIO DE EJECUCION", S['h1']), HR(),
          cal_table([
              ['Semana','Fechas','Hito principal'],
              ['S1-S3','25 may - 14 jun','Fundamentos: layout, clasificacion, primeras familias Revit'],
              ['S4-S5','15 - 28 jun','Cierre familias Revit + exportacion a Unreal Engine 5'],
              ['S6-S7','29 jun - 12 jul','Renders + maquetacion final InDesign'],
              ['S8','13 - 19 jul','Envio PDF preliminar a Metalperfil — HITO DE CONTROL'],
              ['S9','20 - 26 jul','Incorporacion de correcciones'],
              ['S10','27 - 31 jul','Entrega final + emision de factura (13.000 EUR + IVA)'],
          ], highlight_row=4),
          SP(16)]
 
    s += [Paragraph("6. CIERRE", S['h1']), HR(),
          Paragraph("El presente documento formaliza el alcance tecnico y economico del Catalogo Tecnico de Productos de Metalperfil en su Version 1.0.", S['n']),
          SP(6),
          Paragraph("El desarrollo propuesto posiciona el catalogo como una pieza de comunicacion tecnica de alto valor, integrando visualizacion arquitectonica de nivel industrial y documentacion parametrica reutilizable por el equipo de Metalperfil.", S['n'])]
 
    doc.build(s)
    print(f"OK: {path}")
 
 
# ===============================================================
# PDF 2 — AMPLIACION SERVICIOS CATALOGO METALPERFIL
# ===============================================================
def pdf_ampliacion_catalogo(path):
    doc = make_doc(path)
    s = []
    s += [SP(20), Paragraph("Ecosistema Digital Metalperfil", S['tm']),
          SP(8), Paragraph("PROPUESTA DE EVOLUCION", S['ts']),
          SP(30), logo_cover(), SP(20),
          Paragraph("<i>Consultor: Miguel Suarez Torres - Arquitecto / BIM &amp; Automation Specialist</i>", S['mt']),
          Paragraph("<i>Malaga, Espana, 25 de Mayo de 2026</i>", S['mt']),
          SP(30), NextPageTemplate('Later'), PageBreak()]
 
    s += [Paragraph("INDICE", S['h2']), HR()]
    for i, t in enumerate(["Introduccion","EXTRA 1 — Programa caminable interactivo (Unreal Engine 5)",
                            "EXTRA 2 — Experiencia de Realidad Virtual (VR)",
                            "EXTRA 3 — Pagina web del catalogo",
                            "Resumen economico evolutivo","Estrategia recomendada"], 1):
        s.append(Paragraph(f"{i}.  {t}", S['n']))
    s.append(SP(20))
 
    s += [Paragraph("1. Introduccion", S['h1']), HR(),
          Paragraph("Tras la entrega del Catalogo Tecnico Version 1.0, y una vez validados los activos digitales generados durante el proyecto (familias Revit, modelos UE5, carta RAL), se identifican tres lineas de evolucion natural orientadas a maximizar el retorno de la inversion ya realizada:", S['n']), SP(6)]
    for t in ["Transformar los modelos estaticos en experiencias interactivas navegables.",
              "Ampliar el canal de distribucion del catalogo al entorno web.",
              "Diferenciar el producto de Metalperfil frente a la competencia mediante tecnologia inmersiva."]:
        s.append(B(t))
    s += [SP(6), Paragraph("Los tres extras son independientes entre si y pueden activarse en cualquier orden.", S['n']), SP(16)]
 
    s += [Paragraph("2. EXTRA 1 — Programa caminable interactivo (Unreal Engine 5)", S['h1']), HR(),
          Paragraph("Objetivo", S['h3']),
          Paragraph("Crear una aplicacion ejecutable (.exe) que permita al equipo comercial y a sus clientes navegar e interactuar en tiempo real con los productos del catalogo dentro de un espacio arquitectonico virtual.", S['n']),
          SP(8), Paragraph("Alcance tecnico", S['h3'])]
    for t in ["Construccion de un entorno arquitectonico de referencia en UE5 donde se exhiben los productos.",
              "Integracion de los shaders y materiales RAL desarrollados durante el catalogo.",
              "Sistema de navegacion en primera persona (WASD + raton) con interfaz minima.",
              "Selector de productos y variantes de acabado (RAL) en tiempo real.",
              "Configuracion de iluminacion de dia/noche y entornos HDRI intercambiables.",
              "Exportacion como aplicacion ejecutable standalone (.exe, Windows) sin dependencias externas."]:
        s.append(B(t))
    s += [SP(8), Paragraph("Impacto", S['h3'])]
    for t in ["Herramienta de venta directa para el equipo comercial.",
              "Experiencia diferenciadora frente a competidores con catalogos estaticos.",
              "Reutilizacion total de los activos UE5 ya producidos — sin coste adicional de modelado."]:
        s.append(B(t))
    s += [SP(8), Paragraph("Estimacion de esfuerzo", S['h3']), Paragraph("40-50 horas.", S['n']), SP(4),
          Paragraph("Presupuesto propuesto", S['h3']), Paragraph("<b>3.200 EUR + IVA</b>", S['b']), SP(16)]
 
    s += [Paragraph("3. EXTRA 2 — Experiencia de Realidad Virtual (VR)", S['h1']), HR(),
          Paragraph("Objetivo", S['h3']),
          Paragraph("Adaptar el programa caminable a un entorno de realidad virtual compatible con dispositivos standalone (Meta Quest) y PC-VR, permitiendo una presentacion inmersiva en ferias, showrooms y visitas comerciales.", S['n']),
          SP(8), Paragraph("Alcance tecnico", S['h3'])]
    for t in ["Optimizacion del entorno UE5 para renderizado VR (90fps estables en Meta Quest 3).",
              "Configuracion del modo XR y controladores de mano para navegacion intuitiva.",
              "Adaptacion de la interfaz de seleccion de productos para interaccion con mandos VR.",
              "Compilacion para plataforma Android (Meta Quest standalone) y PC-VR.",
              "Pruebas de rendimiento y ajuste de niveles de detalle (LOD)."]:
        s.append(B(t))
    s += [SP(8), Paragraph("Consideraciones", S['h3']),
          Paragraph("Requiere tener activo o completado el Extra 1 como base tecnica. Incluye hasta 2 ciclos de revision en dispositivo; iteraciones adicionales se facturaran a tarifa hora.", S['n']),
          SP(8), Paragraph("Estimacion de esfuerzo", S['h3']), Paragraph("30-40 horas (sobre la base del Extra 1).", S['n']), SP(4),
          Paragraph("Presupuesto propuesto", S['h3']), Paragraph("<b>2.500 EUR + IVA</b><br/>(Requiere Extra 1 activado previamente.)", S['b']), SP(16)]
 
    s += [Paragraph("4. EXTRA 3 — Pagina web del catalogo", S['h1']), HR(),
          Paragraph("Objetivo", S['h3']),
          Paragraph("Crear una pagina web de producto que funcione como version digital interactiva del catalogo, con navegacion por categorias, fichas de producto con renders, selector de acabados RAL y descarga del PDF.", S['n']),
          SP(8), Paragraph("Alcance tecnico", S['h3'])]
    for t in ["Desarrollo de sitio estatico (HTML/CSS/JS o Webflow) sin necesidad de servidor ni base de datos.",
              "Estructura de navegacion por familia de producto (paneles, esquinas, microperforados, etc.).",
              "Ficha individual por producto con renders UE5, especificaciones tecnicas y carta RAL interactiva.",
              "Formulario de contacto y descarga del PDF de catalogo.",
              "Diseno responsivo (desktop, tablet, movil).",
              "Entrega de codigo fuente y despliegue en dominio proporcionado por Metalperfil."]:
        s.append(B(t))
    s += [SP(8), Paragraph("Impacto", S['h3'])]
    for t in ["Canal de distribucion digital autonomo del catalogo.",
              "Herramienta de captacion y referencia para prescriptores y estudios de arquitectura.",
              "Reutilizacion directa de todos los renders e imagenes del catalogo."]:
        s.append(B(t))
    s += [SP(8), Paragraph("Estimacion de esfuerzo", S['h3']), Paragraph("35-45 horas.", S['n']), SP(4),
          Paragraph("Presupuesto propuesto", S['h3']), Paragraph("<b>2.500 EUR + IVA</b>", S['b']), SP(16)]
 
    s += [Paragraph("5. Resumen Economico Evolutivo", S['h1']), HR()]
    evo = [['Extra','Descripcion','Presupuesto'],
           ['Extra 1','Programa caminable interactivo (UE5)','3.200 EUR'],
           ['Extra 2','Experiencia VR (sobre Extra 1)','2.500 EUR'],
           ['Extra 3','Pagina web del catalogo','2.500 EUR'],
           ['','TOTAL EVOLUCION COMPLETA','8.200 EUR + IVA']]
    evo_t = Table(evo, colWidths=[2.5*cm, 10.5*cm, 3.5*cm])
    evo_t.setStyle(TableStyle([
        ('FONTNAME',(0,0),(-1,0),'Helvetica-Bold'),('FONTNAME',(0,1),(-1,-2),'Helvetica'),
        ('FONTNAME',(0,-1),(-1,-1),'Helvetica-Bold'),('FONTSIZE',(0,0),(-1,-1),10),
        ('TEXTCOLOR',(0,0),(-1,-1),BLACK),('ROWBACKGROUNDS',(0,1),(-1,-2),[WHITE,LIGHT]),
        ('BACKGROUND',(0,0),(-1,0),LIGHT),('BACKGROUND',(0,-1),(-1,-1),LIGHT),
        ('LINEBELOW',(0,0),(-1,0),0.5,LINE),('LINEABOVE',(0,-1),(-1,-1),0.5,LINE),
        ('TOPPADDING',(0,0),(-1,-1),6),('BOTTOMPADDING',(0,0),(-1,-1),6),
        ('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),
        ('ALIGN',(2,0),(2,-1),'RIGHT'),('VALIGN',(0,0),(-1,-1),'MIDDLE'),
    ]))
    s += [evo_t, SP(16),
          Paragraph("6. Estrategia Recomendada", S['h1']), HR(),
          Paragraph("Se recomienda:", S['n']), SP(4)]
    for i, t in enumerate([
        "Entregar el catalogo base (julio) y permitir que Metalperfil lo valore internamente.",
        "Activar el Extra 3 (web) como primer paso — menor inversion, mayor impacto inmediato.",
        "Evaluar el Extra 1 (caminable) tras validar el uso comercial del catalogo.",
        "El Extra 2 (VR) queda como opcion avanzada para una segunda fase si el Extra 1 demuestra traccion.",
    ], 1):
        s.append(Paragraph(f"{i}.  {t}", S['n']))
 
    doc.build(s)
    print(f"OK: {path}")
 
 
# ===============================================================
# PDF 3 — OFERTA HONORARIOS CIP ARQUITECTOS
# ===============================================================
def pdf_cip(path):
    doc = make_doc(path)
    s = []
    s += [SP(20), Paragraph("Consultoria BIM y Automatizacion de Flujos<br/>CIP Arquitectos", S['tm']),
          SP(8), Paragraph("Oferta de honorarios", S['ts']), Paragraph("Version 1.0", S['ts']),
          SP(30), logo_cover(), SP(20),
          Paragraph("<i>Consultor: Miguel Suarez Torres - Arquitecto / BIM &amp; Automation Specialist</i>", S['mt']),
          Paragraph("<i>Malaga, Espana, 25 de Mayo de 2026</i>", S['mt']),
          SP(30), NextPageTemplate('Later'), PageBreak()]
 
    s += [Paragraph("INDICE", S['h2']), HR()]
    for i, t in enumerate(["Objeto y estructura del encargo",
                            "WORKSTREAM 1 — Consultoria de optimizacion BIM",
                            "WORKSTREAM 2 — Plugin de extraccion Revit-Presto",
                            "Limites del encargo",
                            "Consolidacion economica",
                            "Calendario de ejecucion",
                            "Cierre"], 1):
        s.append(Paragraph(f"{i}.  {t}", S['n']))
    s.append(SP(20))
 
    s += [Paragraph("1. OBJETO Y ESTRUCTURA DEL ENCARGO", S['h1']), HR(),
          Paragraph("El presente encargo se estructura en dos lineas de trabajo independientes y complementarias, ejecutadas de forma simultanea a lo largo de nueve semanas.", S['n']),
          SP(6),
          Paragraph("El primer workstream es una consultoria de optimizacion BIM con metodologia ciclica de observacion y accion. El segundo es el desarrollo de una herramienta tecnica — plugin o script externo — para la extraccion automatizada de informacion desde Revit y su volcado estructurado en Presto.", S['n']),
          SP(6),
          Paragraph("Ambos workstreams convergen en la semana final con la formacion del equipo y la entrega de documentacion tecnica.", S['n']),
          SP(16)]
 
    s += [Paragraph("2. WORKSTREAM 1 — Consultoria de optimizacion BIM", S['h1']), HR(),
          Paragraph("Objeto", S['h3']),
          Paragraph("Identificar ineficiencias en los procesos de produccion interna de CIP Arquitectos y desarrollar acciones correctivas concretas, con especial enfoque en el uso de Revit y las herramientas del ecosistema BIM del estudio.", S['n']),
          SP(8), Paragraph("Metodologia: ciclos observar-actuar", S['h3']),
          Paragraph("El trabajo se organiza en dos ciclos de dos semanas cada uno. En la primera semana de cada ciclo se observa y diagnostica; en la segunda se actua e implementa. Al cierre de cada ciclo se entrega un informe de resultados.", S['n']),
          SP(8)]
 
    ciclos = [
        ['Ciclo','Semanas','Observar (sem. impar)','Actuar (sem. par)'],
        ['Ciclo 1','S1-S2','Mapeo de flujos, deteccion de fricciones en produccion',
         'Implementacion de mejoras priorizadas con el equipo'],
        ['Ciclo 2','S3-S4','Revision de resultados del ciclo 1, nuevas areas de mejora',
         'Implementacion de segunda ronda de optimizaciones'],
    ]
    ciclos_t = Table(ciclos, colWidths=[1.5*cm, 2*cm, 6.5*cm, 6.5*cm])
    ciclos_t.setStyle(TableStyle([
        ('FONTNAME',(0,0),(-1,0),'Helvetica-Bold'),('FONTNAME',(0,1),(-1,-1),'Helvetica'),
        ('FONTSIZE',(0,0),(-1,-1),9),('TEXTCOLOR',(0,0),(-1,-1),BLACK),
        ('ROWBACKGROUNDS',(0,1),(-1,-1),[WHITE,LIGHT]),('BACKGROUND',(0,0),(-1,0),LIGHT),
        ('LINEBELOW',(0,0),(-1,0),0.5,LINE),
        ('TOPPADDING',(0,0),(-1,-1),5),('BOTTOMPADDING',(0,0),(-1,-1),5),
        ('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),
        ('VALIGN',(0,0),(-1,-1),'MIDDLE'),
    ]))
    s += [ciclos_t, SP(8), Paragraph("Entregables", S['h3'])]
    for t in ["Informe de cierre del Ciclo 1: diagnostico, acciones implementadas y resultados.",
              "Informe de cierre del Ciclo 2: diagnostico, acciones implementadas y resultados.",
              "Hoja de ruta de optimizacion BIM para continuidad autonoma del estudio."]:
        s.append(B(t))
    s.append(SP(16))
 
    s += [Paragraph("3. WORKSTREAM 2 — Plugin de extraccion Revit-Presto", S['h1']), HR(),
          Paragraph("Objeto", S['h3']),
          Paragraph("Desarrollar una herramienta — plugin nativo o script externo integrado via PyRevit — que extraiga automaticamente informacion geometrica y de identidad desde modelos Revit y la vuelque en la estructura de Presto del estudio, eliminando la intervencion manual en ese proceso.", S['n']),
          SP(8), Paragraph("Fases de desarrollo", S['h3'])]
    for t in ["Especificacion (semanas 1-2): analisis del modelo Revit de referencia, mapeo de parametros y entrega del documento 'Estandar de Parametros de Identidad Compartidos'.",
              "Desarrollo (semanas 3-7): construccion del nucleo de extraccion, integracion con Presto, gestion de excepciones y hito de revision tecnica con el equipo en semana 6.",
              "Integracion (semana 8): interfaz PyRevit o launcher externo, pruebas de estres con modelos reales del estudio."]:
        s.append(B(t))
    s += [SP(8), Paragraph("Entregables", S['h3'])]
    for t in ["Script o plugin funcional con codigo documentado y arquitectura modular.",
              "Flujo de trabajo establecido y validado con el equipo.",
              "Manual tecnico de uso y mantenimiento."]:
        s.append(B(t))
    s.append(SP(16))
 
    s += [Paragraph("4. LIMITES DEL ENCARGO", S['h1']), HR(),
          Paragraph("Con el fin de garantizar la viabilidad del proyecto dentro del presupuesto acordado, el encargo queda expresamente acotado por las siguientes condiciones:", S['n']),
          SP(8),
          Paragraph("4.1 Limites de la consultoria BIM", S['h3']), SP(4),
          limit_box([
              "La consultoria cubre exactamente 2 ciclos de 2 semanas (4 semanas en total). Cualquier ciclo adicional se presupuestara como fase independiente.",
              "Las acciones de optimizacion se limitan a los flujos y herramientas actualmente en uso en el estudio. La adopcion de nuevas plataformas o licencias queda fuera del alcance.",
              "El bloque de consultoria no incluye desarrollo de herramientas. Cualquier herramienta identificada se presupuestara de forma independiente.",
          ]),
          SP(12),
          Paragraph("4.2 Limites del plugin Revit-Presto", S['h3']), SP(4),
          limit_box([
              "El desarrollo cubre la estructura de Revit y Presto vigente en el estudio a la fecha de inicio. Cambios posteriores en dicha configuracion constituyen una nueva fase.",
              "El plugin se desarrolla y valida sobre los modelos Revit aportados por el estudio durante el proyecto. Tipologias no cubiertas en esa muestra pueden requerir ajustes adicionales facturados a tarifa hora.",
              "El Workstream 2 no incluye formacion avanzada en Python ni en la API de Revit para el equipo del estudio.",
          ]),
          SP(16)]
 
    s += [Paragraph("5. CONSOLIDACION ECONOMICA", S['h1']), HR(),
          fee_table([
              ['WS1','Consultoria BIM — 2 ciclos observar-actuar + informes de cierre','1.800 EUR'],
              ['WS2-A','Especificacion tecnica y documento de estandar de parametros','Incluido'],
              ['WS2-B','Desarrollo del plugin/script de extraccion Revit-Presto','2.400 EUR'],
              ['WS2-C','Integracion PyRevit + interfaz de usuario + pruebas de estres','600 EUR'],
              ['Cierre','Formacion equipo (2-3h) + manual tecnico + documentacion','Incluido'],
          ], 'TOTAL PROYECTO - Version 1.0', '4.800 EUR + IVA'),
          SP(10),
          Paragraph("El importe total del proyecto asciende a <b>4.800 EUR + IVA (21%)</b>, resultando una factura total de 5.808 EUR. Facturacion en dos emisiones: 50% al inicio del WS2-B (semana 3) y 50% a la entrega final (semana 9).", S['n']),
          SP(16)]
 
    s += [Paragraph("6. CALENDARIO DE EJECUCION", S['h1']), HR(),
          cal_table([
              ['Semana','Fechas','WS1 — Consultoria / WS2 — Plugin'],
              ['S1','26 may - 1 jun','WS1: Observar ciclo 1 / WS2: Especificacion y mapeo de parametros'],
              ['S2','2 - 8 jun',     'WS1: Actuar ciclo 1 / WS2: Entrega Estandar de Parametros — HITO'],
              ['S3','9 - 15 jun',    'WS1: Observar ciclo 2 / WS2: Desarrollo nucleo script'],
              ['S4','16 - 22 jun',   'WS1: Actuar ciclo 2 + Informe final WS1 / WS2: Desarrollo continua'],
              ['S5-S6','23 jun - 5 jul','WS2: Desarrollo e integracion Presto'],
              ['S6','5 jul',         'WS2: Hito de revision tecnica con equipo CIP — HITO'],
              ['S7','6 - 12 jul',    'WS2: Depuracion con modelos reales del estudio'],
              ['S8','13 - 19 jul',   'WS2: Integracion PyRevit + interfaz + pruebas de estres'],
              ['S9','20 - 26 jul',   'Formacion equipo + entrega total + emision factura final'],
          ], highlight_row=2),
          SP(16)]
 
    s += [Paragraph("7. CIERRE", S['h1']), HR(),
          Paragraph("El presente documento formaliza el alcance tecnico y economico de la consultoria BIM y desarrollo de herramientas de automatizacion para CIP Arquitectos en su Version 1.0.", S['n']),
          SP(6),
          Paragraph("El sistema desarrollado eliminara la intervencion manual en el flujo Revit-Presto y dotara al estudio de una hoja de ruta clara para la optimizacion continua de sus procesos de produccion.", S['n']),
          SP(6),
          Paragraph("Cualquier evolucion futura — ciclos adicionales de consultoria, nuevas tipologias en el plugin, o desarrollo de un asistente de IA para automatizacion de memorias CTE — se presupuestara como fase independiente.", S['n'])]
 
    doc.build(s)
    print(f"OK: {path}")
 
 
# ── RUN ───────────────────────────────────────────────────────
pdf_oferta_catalogo('/mnt/user-data/outputs/Oferta_Honorarios_Miguel_Metalperfil_Catalogo.pdf')
pdf_ampliacion_catalogo('/mnt/user-data/outputs/Ampliacion_Servicios_Miguel_Metalperfil_Catalogo.pdf')
pdf_cip('/mnt/user-data/outputs/Oferta_Honorarios_Miguel_CIP_Arquitectos.pdf')
