"""
create_revit_families_pro.py
Versión optimizada para Revit 2026 (.NET 8).
Sin dependencias externas de XAML y con gestión robusta de parámetros.
"""

import clr
import os
import json
import math

# Referencias para Revit API
clr.AddReference("RevitAPI")
clr.AddReference("RevitAPIUI")

# Referencias para la Interfaz (WPF Puro)
clr.AddReference("PresentationFramework")
clr.AddReference("PresentationCore")
clr.AddReference("WindowsBase")
clr.AddReference("System")

from Autodesk.Revit.DB import (
    Transaction, SaveAsOptions, XYZ, UV, Line, Arc, Plane, CurveArray, 
    CurveArrArray, SketchPlane, GroupTypeId, SpecTypeId, 
    BuiltInParameter, FilteredElementCollector, ReferencePlane,
    Options, Solid, PlanarFace, View, ViewType, ViewDetailLevel,
    ElementTransformUtils
)

# Importamos controles de WPF
import System.Windows.Controls as Controls
from System.Windows import Window, WindowStartupLocation, Thickness, HorizontalAlignment, VerticalAlignment, GridLength, MessageBox, FontWeights

# ── Configuración Dinámica ───────────────────────────────────────────────────

# Intentamos detectar la ruta del script para encontrar el JSON
try:
    _HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    # Ruta por defecto si se ejecuta desde consola interactiva
    _HERE = r"D:\Arquitectura\W_TRABAJOS\12_IA_OPT\2601_METALPERFIL_Catalogo\02_PROYECTO\01_Familias de Piezas metálicas"

JSON_PATH    = os.path.join(_HERE, "perfiles_test.json") 
OUTPUT_DIR   = os.path.join(_HERE, "Familias_Revit")
# Plantilla estándar para Revit 2026
TEMPLATE_PATH = r"C:\ProgramData\Autodesk\RVT 2026\Family Templates\English\Metric Curtain Wall Panel.rft"

GROSOR_MM    = 2.0
ALTURA_MM    = 3000.0   
MM_TO_FT     = 1.0 / 304.8

# ── Utilidades de Conversión y Geometría ──────────────────────────────────────

def ft(mm): return mm * MM_TO_FT

def normalize(vertices):
    """Centra el perfil en el origen (0,0) para evitar desfases en Revit"""
    if not vertices: return []
    min_x, max_x = min(v[0] for v in vertices), max(v[0] for v in vertices)
    min_y, max_y = min(v[1] for v in vertices), max(v[1] for v in vertices)
    cx, cy = (min_x + max_x) / 2.0, (min_y + max_y) / 2.0
    return [(v[0] - cx, v[1] - cy, v[2] if len(v) > 2 else 0.0) for v in vertices]

def offset_polyline(vertices, distance):
    """Calcula el contorno paralelo para generar el grosor de la chapa"""
    n = len(vertices)
    if n < 2: return vertices
    seg_normals = []
    for i in range(n - 1):
        dx, dy = vertices[i + 1][0] - vertices[i][0], vertices[i + 1][1] - vertices[i][1]
        length = math.hypot(dx, dy)
        if length < 1e-10: seg_normals.append((0.0, 1.0))
        else: seg_normals.append((-dy / length, dx / length))
    
    vnormals = []
    for i in range(n):
        if i == 0: nx, ny = seg_normals[0]
        elif i == n - 1: nx, ny = seg_normals[-1]
        else:
            nx, ny = seg_normals[i - 1][0] + seg_normals[i][0], seg_normals[i - 1][1] + seg_normals[i][1]
            mag = math.hypot(nx, ny)
            if mag > 1e-10: nx, ny = nx / mag, ny / mag
            else: nx, ny = seg_normals[i]
        vnormals.append((nx, ny))
    return [(vertices[i][0] + distance * vnormals[i][0], vertices[i][1] + distance * vnormals[i][1]) for i in range(n)]

def make_closed_loop(norm, off_2d):
    closed = []
    n = len(norm)
    # Forward path: V_1 to V_n
    for i in range(n - 1):
        closed.append((norm[i][0], norm[i][1], norm[i][2]))
    closed.append((norm[-1][0], norm[-1][1], 0.0))
    
    # Backward path: U_n to U_1
    for i in range(n - 1, 0, -1):
        closed.append((off_2d[i][0], off_2d[i][1], -norm[i-1][2]))
    closed.append((off_2d[0][0], off_2d[0][1], 0.0))
    
    # Connect back to start V_1
    closed.append((norm[0][0], norm[0][1], 0.0))
    return closed

def make_curve_array(app, pts_mm):
    """Convierte puntos en líneas y arcos de Revit respetando la tolerancia ShortCurveTolerance"""
    tol = app.ShortCurveTolerance * 1.1
    arr = CurveArray()
    if not pts_mm: return arr
    last_pt = XYZ(ft(pts_mm[0][0]), ft(pts_mm[0][1]), 0.0)
    for i in range(1, len(pts_mm)):
        curr_pt = XYZ(ft(pts_mm[i][0]), ft(pts_mm[i][1]), 0.0)
        if last_pt.DistanceTo(curr_pt) > tol:
            bulge = pts_mm[i-1][2] if len(pts_mm[i-1]) > 2 else 0.0
            if abs(bulge) < 1e-5:
                arr.Append(Line.CreateBound(last_pt, curr_pt))
                last_pt = curr_pt
            else:
                # Calculate midpoint of the arc
                x1, y1 = pts_mm[i-1][0], pts_mm[i-1][1]
                x2, y2 = pts_mm[i][0], pts_mm[i][1]
                mx = (x1 + x2) / 2.0 - bulge * (y2 - y1) / 2.0
                my = (y1 + y2) / 2.0 + bulge * (x2 - x1) / 2.0
                mid_pt = XYZ(ft(mx), ft(my), 0.0)
                
                if last_pt.DistanceTo(mid_pt) > tol and mid_pt.DistanceTo(curr_pt) > tol:
                    try:
                        arr.Append(Arc.Create(last_pt, curr_pt, mid_pt))
                        last_pt = curr_pt
                    except Exception:
                        arr.Append(Line.CreateBound(last_pt, curr_pt))
                        last_pt = curr_pt
                else:
                    arr.Append(Line.CreateBound(last_pt, curr_pt))
                    last_pt = curr_pt
    return arr

# ── Lógica de Revit ──────────────────────────────────────────────────────────

def create_rfa(app, family_name, type_name, vertices_mm):
    # 1. Procesar Geometría
    norm         = normalize(vertices_mm)
    off          = offset_polyline(norm, GROSOR_MM)
    closed_pts   = make_closed_loop(norm, off)
    
    if not os.path.exists(TEMPLATE_PATH):
        raise Exception("Plantilla no encontrada: " + TEMPLATE_PATH)

    family_doc = app.NewFamilyDocument(TEMPLATE_PATH)
    
    t = Transaction(family_doc, "Crear Perfil Pro 2026")
    t.Start()
    try:
        mgr = family_doc.FamilyManager
        # Parámetros con IDs de Forge modernos (compatibles con RVT 2022-2026)
        p_grosor = mgr.AddParameter("Grosor", GroupTypeId.Geometry, SpecTypeId.Length, False)
        p_altura = mgr.AddParameter("Altura", GroupTypeId.Geometry, SpecTypeId.Length, False)
        p_material = mgr.AddParameter("Material del Panel", GroupTypeId.Materials, SpecTypeId.Reference.Material, False)
        
        mgr.NewType(type_name)
        mgr.Set(p_grosor, ft(GROSOR_MM))
        mgr.Set(p_altura, ft(ALTURA_MM))

        # Obtener planos de referencia
        planes = FilteredElementCollector(family_doc).OfClass(ReferencePlane).ToElements()
        ref_planes = {p.Name: p for p in planes}
        
        left_plane = ref_planes.get("Left")
        right_plane = ref_planes.get("Right")
        top_plane = ref_planes.get("Top")
        
        # Calcular límites de la geometría
        xs = [v[0] for v in norm]
        x_min_ft = ft(min(xs))
        x_max_ft = ft(max(xs))
        
        # Mover los planos "Left", "Right" y "Top" para coincidir con el modelo
        if left_plane:
            dx = x_min_ft - left_plane.GetPlane().Origin.X
            ElementTransformUtils.MoveElement(family_doc, left_plane.Id, XYZ(dx, 0.0, 0.0))
        if right_plane:
            dx = x_max_ft - right_plane.GetPlane().Origin.X
            ElementTransformUtils.MoveElement(family_doc, right_plane.Id, XYZ(dx, 0.0, 0.0))
        if top_plane:
            dz = ft(ALTURA_MM) - top_plane.GetPlane().Origin.Z
            ElementTransformUtils.MoveElement(family_doc, top_plane.Id, XYZ(0.0, 0.0, dz))

        # Crear plano de boceto y extrusión
        sketch_plane = SketchPlane.Create(family_doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero))
        curve_arr = make_curve_array(app, closed_pts)
        profile = CurveArrArray()
        profile.Append(curve_arr)
        
        try:
            extr = family_doc.FamilyCreate.NewExtrusion(True, profile, sketch_plane, ft(ALTURA_MM))
        except Exception as e:
            print("Error fatal creando extrusión para {}_{}: {}".format(family_name, type_name, e))
            raise e
        
        # Vincular la altura de la extrusión al parámetro de la familia
        p_end = extr.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM)
        if p_end:
            mgr.AssociateElementParameterToFamilyParameter(p_end, p_altura)
            
        # Vincular el material de la extrusión al parámetro de la familia
        p_extr_mat = extr.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM)
        if p_extr_mat:
            mgr.AssociateElementParameterToFamilyParameter(p_extr_mat, p_material)

        # Configurar subcategoría "Paneles de Fachada"
        try:
            category = family_doc.OwnerFamily.FamilyCategory
            subcats = category.SubCategories
            subcat_name = "Paneles de Fachada"
            subcat = None
            if subcats.Contains(subcat_name):
                subcat = subcats.get_Item(subcat_name)
            else:
                subcat = family_doc.Settings.Categories.NewSubcategory(category, subcat_name)
                
            p_subcat = extr.get_Parameter(BuiltInParameter.FAMILY_ELEM_SUBCATEGORY)
            if p_subcat and subcat:
                p_subcat.Set(subcat.Id)
        except Exception as ex_subcat:
            print("Aviso: No se pudo configurar la subcategoría: {}".format(ex_subcat))

        # Alinear planos a las caras y bloquear
        opt = Options()
        opt.ComputeReferences = True
        geom = extr.get_Geometry(opt)
        
        top_face_ref = None
        left_face_ref = None
        right_face_ref = None
        
        for geom_obj in geom:
            if isinstance(geom_obj, Solid):
                for face in geom_obj.Faces:
                    # IGNORAR CARAS SIN NORMAL (ej. caras cilíndricas en este contexto)
                    if not hasattr(face, "FaceNormal"):
                        continue
                        
                    normal = face.FaceNormal
                    if normal.IsAlmostEqualTo(XYZ.BasisZ):
                        top_face_ref = face.Reference
                    
                    if abs(normal.Y) < 1e-5 and abs(normal.Z) < 1e-5:
                        bbox = face.GetBoundingBox()
                        face_center = face.Evaluate(UV((bbox.Min.U + bbox.Max.U)/2.0, (bbox.Min.V + bbox.Max.V)/2.0))
                        if abs(face_center.X - x_min_ft) < 1e-3:
                            left_face_ref = face.Reference
                        elif abs(face_center.X - x_max_ft) < 1e-3:
                            right_face_ref = face.Reference

        # Se eliminó la lógica de alineación física manual (NewAlignment) ya que causaba errores 
        # y no es necesaria debido a la asociación paramétrica definida arriba.

        t.Commit()
    except Exception as e:
        t.RollBack()
        family_doc.Close(False)
        raise e

    # Guardado seguro
    out_dir = os.path.join(OUTPUT_DIR, family_name)
    if not os.path.isdir(out_dir): os.makedirs(out_dir)
    
    safe_fam = family_name.replace("/","_").replace("\\","_")
    safe_typ = type_name.replace("/","_").replace("\\","_")
    filepath = os.path.join(out_dir, "{0}_{1}.rfa".format(safe_fam, safe_typ))
    
    opts = SaveAsOptions()
    opts.OverwriteExistingFile = True
    family_doc.SaveAs(filepath, opts)
    family_doc.Close(False)
    return filepath

# ── Interfaz de Usuario (UI) ─────────────────────────────────────────────────

class ProfileItem:
    def __init__(self, family, type_name, vertices):
        self.Family = family
        self.Type = type_name
        self.Vertices = vertices
    def ToString(self):
        return "[{}] {}".format(self.Family, self.Type)
    def __str__(self):
        return self.ToString()

class ProfileSelector:
    def __init__(self, data):
        self.Root = Window()
        self.Root.Title = "Metalperfil RFA Generator 2026"
        self.Root.Width = 400
        self.Root.Height = 550
        self.Root.WindowStartupLocation = WindowStartupLocation.CenterScreen
        self.Root.Topmost = True

        grid = Controls.Grid()
        grid.Margin = Thickness(15)
        self.Root.Content = grid

        grid.RowDefinitions.Add(Controls.RowDefinition()) # Titulo
        grid.RowDefinitions[0].Height = GridLength.Auto
        grid.RowDefinitions.Add(Controls.RowDefinition()) # Filtro
        grid.RowDefinitions[1].Height = GridLength.Auto
        grid.RowDefinitions.Add(Controls.RowDefinition()) # Lista
        grid.RowDefinitions.Add(Controls.RowDefinition()) # Boton
        grid.RowDefinitions[3].Height = GridLength.Auto

        lbl = Controls.TextBlock()
        lbl.Text = "Selecciona los perfiles a generar:"
        lbl.FontWeight = FontWeights.Bold
        lbl.Margin = Thickness(0,0,0,10)
        grid.Children.Add(lbl)

        self.txtFilter = Controls.TextBox()
        self.txtFilter.Margin = Thickness(0,0,0,5)
        self.txtFilter.Padding = Thickness(3)
        self.txtFilter.TextChanged += self.on_filter_changed
        grid.Children.Add(self.txtFilter)
        Controls.Grid.SetRow(self.txtFilter, 1)

        self.lstPerfiles = Controls.ListBox()
        self.lstPerfiles.SelectionMode = Controls.SelectionMode.Multiple
        grid.Children.Add(self.lstPerfiles)
        Controls.Grid.SetRow(self.lstPerfiles, 2)

        self.btnImport = Controls.Button()
        self.btnImport.Content = "GENERAR FAMILIAS"
        self.btnImport.Height = 40
        self.btnImport.FontWeight = FontWeights.Bold
        self.btnImport.Margin = Thickness(0,10,0,0)
        self.btnImport.Click += self.on_import_click
        grid.Children.Add(self.btnImport)
        Controls.Grid.SetRow(self.btnImport, 3)

        self.all_items = []
        for fam, types in data.items():
            for t in types:
                self.all_items.append(ProfileItem(fam, t["type"], t["vertices"]))
        
        self.lstPerfiles.ItemsSource = self.all_items
        self.SelectedItems = []

    def on_filter_changed(self, sender, e):
        filt = self.txtFilter.Text.lower()
        self.lstPerfiles.ItemsSource = [i for i in self.all_items if filt in i.Family.lower() or filt in i.Type.lower()]

    def on_import_click(self, sender, e):
        self.SelectedItems = list(self.lstPerfiles.SelectedItems)
        if not self.SelectedItems:
            MessageBox.Show("Por favor, selecciona al menos un perfil de la lista.")
            return
        self.Root.DialogResult = True
        self.Root.Close()

    def show(self):
        return self.Root.ShowDialog()

# ── Ejecución Principal ──────────────────────────────────────────────────────

def main():
    try:
        app = __revit__.Application
    except NameError:
        print("ERROR: Este script debe ejecutarse desde Revit (pyRevit o RevitPythonShell).")
        return

    if not os.path.exists(JSON_PATH):
        MessageBox.Show("No se encontró el archivo de datos JSON en:\n" + JSON_PATH)
        return

    with open(JSON_PATH, "r") as f:
        data = json.load(f)

    selector = ProfileSelector(data)
    if selector.show():
        ok, err = 0, 0
        for item in selector.SelectedItems:
            try:
                path = create_rfa(app, item.Family, item.Type, item.Vertices)
                print("[OK] " + path)
                ok += 1
            except Exception as e:
                print("[ERROR] {}: {}".format(item.Type, e))
                err += 1
        
        MessageBox.Show("Proceso finalizado.\n\nExitosos: {}\nErrores: {}".format(ok, err))

if __name__ == "__main__":
    main()
