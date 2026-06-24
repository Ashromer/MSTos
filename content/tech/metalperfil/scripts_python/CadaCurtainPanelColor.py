import random
import clr
clr.AddReference("RevitAPI")
from Autodesk.Revit.DB import *

doc = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument
view = doc.ActiveView

# 1. Obtener paneles de los muros cortina seleccionados
paneles_por_tipo = {}
for el_id in uidoc.Selection.GetElementIds():
    muro = doc.GetElement(el_id)
    if isinstance(muro, Wall) and muro.WallType.Kind == WallKind.Curtain and muro.CurtainGrid:
        for p_id in muro.CurtainGrid.GetPanelIds():
            p = doc.GetElement(p_id)
            if p and p.GetTypeId() != ElementId.InvalidElementId:
                paneles_por_tipo.setdefault(p.GetTypeId(), []).append(p)

# 2. Buscar el patrón "Relleno uniforme" de forma segura para Revit en español
id_relleno_uniforme = None
for fp in FilteredElementCollector(doc).OfClass(FillPatternElement):
    # Buscamos por el nombre exacto que usa Revit en español
    if fp.Name == "Relleno uniforme" or fp.Name == "<Relleno uniforme>":
        id_relleno_uniforme = fp.Id
        break

# Si por algún motivo tu plantilla está en inglés, buscamos "Solid fill"
if not id_relleno_uniforme:
    for fp in FilteredElementCollector(doc).OfClass(FillPatternElement):
        if "solid" in fp.Name.lower():
            id_relleno_uniforme = fp.Id
            break

# 3. Aplicar Overrides rellenando el Patrón y el Color
if id_relleno_uniforme:
    t = Transaction(doc, "Color Paneles Uniforme")
    t.Start()

    for type_id, paneles in paneles_por_tipo.items():
        # Generar color aleatorio
        color = Color(random.randint(0,255), random.randint(0,255), random.randint(0,255))
        
        ogs = OverrideGraphicSettings()
        
        # ESTO CAMBIA EL DESPLEGABLE QUE ME MARCAS EN LA CAPTURA:
        ogs.SetSurfaceForegroundPatternId(id_relleno_uniforme)
        ogs.SetSurfaceForegroundPatternColor(color)
        
        # También lo aplicamos al corte por si la vista está en planta/sección cortando el panel
        ogs.SetCutForegroundPatternId(id_relleno_uniforme)
        ogs.SetCutForegroundPatternColor(color)
        
        # Transparencia del 20% como en tu captura
        ogs.SetSurfaceTransparency(20)

        for p in paneles:
            view.SetElementOverrides(p.Id, ogs)

    t.Commit()
    print("¡Conseguido! Patrón cambiado a 'Relleno uniforme' con colores por tipo.")
else:
    print("Error: No se encontró el patrón 'Relleno uniforme' en el proyecto.")