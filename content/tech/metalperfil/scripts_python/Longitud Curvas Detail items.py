import sys
# Clases necesarias de la API de Revit
from Autodesk.Revit.DB import *

# Variables globales de la Shell
doc = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument

def calcular_longitud_lineas():
    # 1. Obtener los elementos seleccionados por el usuario en Revit
    seleccion_ids = uidoc.Selection.GetElementIds()
    
    if not seleccion_ids:
        print("Por favor, selecciona primero las Detail Lines en Revit y vuelve a ejecutar.")
        return

    longitud_total_pies = 0.0
    contador_lineas = 0

    # 2. Recorrer los elementos seleccionados
    for elem_id in seleccion_ids:
        elemento = doc.GetElement(elem_id)
        
        # Verificar si el elemento es una línea de detalle (DetailLine o DetailCurve)
        if isinstance(elemento, DetailCurve):
            # Obtener la geometría pura de la curva (recta, arco, spline, etc.)
            geometria_curva = elemento.GeometryCurve
            
            # Sumar su longitud (la API de Revit siempre mide en PIES internos)
            longitud_total_pies += geometria_curva.Length
            contador_lineas += 1

    if contador_lineas == 0:
        print("De los elementos seleccionados, ninguno era una Detail Line.")
        return

    # 3. CONVERSIÓN SEGURO Y COMPATIBLE (1 pie = 0.3048 metros)
    longitud_en_metros = longitud_total_pies * 0304.8

    print("--- RESULTADO DEL CÁLCULO ---")
    print("Líneas procesadas: {}".format(contador_lineas))
    print("Distancia total: {:.2f} milímetros".format(longitud_en_metros))

# Ejecutar la función
calcular_longitud_lineas()