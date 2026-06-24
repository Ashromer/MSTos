# -*- coding: utf-8 -*-
import clr
import os
import subprocess
import time
import json

# 1. CARGAR ENSAMBLADOS DE LA API DE REVIT
clr.AddReference('RevitAPI')
clr.AddReference('RevitAPIUI')
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI import *

# 2. CARGAR LIBRERÍAS DE WINDOWS PARA LA INTERFAZ GRÁFICA (WPF)
clr.AddReference("PresentationFramework")
clr.AddReference("PresentationCore")
from System.Windows import Window, WindowStartupLocation
from System.Windows.Controls import StackPanel, Button, ListBox, CheckBox

doc = __revit__.ActiveUIDocument.Document

# 3. FUNCIÓN BINARIA PARA ELIMINAR CÁMARAS CORRUPTAS DEL GLB
def limpiar_camaras_gltf(glb_path):
    try:
        with open(glb_path, 'rb') as f:
            data = f.read()
            
        if data[:4] != b'glTF':
            return False
            
        # Leer la longitud del bloque JSON de la cabecera glb
        json_chunk_length = int.from_bytes(data[12:16], 'little')
        if data[16:20] != b'JSON':
            return False
            
        json_bytes = data[20:20+json_chunk_length]
        remaining_data = data[20+json_chunk_length:]
        
        # Descodificar JSON y limpiar propiedades de cámara
        glb_json = json.loads(json_bytes.decode('utf-8'))
        
        if 'cameras' in glb_json:
            del glb_json['cameras']
        if 'nodes' in glb_json:
            for node in glb_json['nodes']:
                if 'camera' in node:
                    del node['camera']
        
        # Volver a serializar a binario
        new_json_bytes = json.dumps(glb_json, separators=(',', ':')).encode('utf-8')
        
        # Rellenar con espacios (0x20) para mantener la alineación de 4 bytes del formato
        pad_len = (4 - (len(new_json_bytes) % 4)) % 4
        new_json_bytes += b' ' * pad_len
        new_json_chunk_length = len(new_json_bytes)
        
        # Recalcular las longitudes de los headers binarios
        new_total_length = 12 + 8 + new_json_chunk_length + len(remaining_data)
        
        header = b'glTF' + int(2).to_bytes(4, 'little') + int(new_total_length).to_bytes(4, 'little')
        json_chunk_header = int(new_json_chunk_length).to_bytes(4, 'little') + b'JSON'
        
        # Sobrescribir el archivo GLB con los datos reparados
        with open(glb_path, 'wb') as f:
            f.write(header + json_chunk_header + new_json_bytes + remaining_data)
        return True
    except Exception as e:
        return False

# 4. RECOLECTAR FAMILIAS DE LA CATEGORÍA PANELES DE MURO CORTINA
all_families = FilteredElementCollector(doc).OfClass(Family)
panel_families = []
target_category_id = ElementId(BuiltInCategory.OST_CurtainWallPanels)

for fam in all_families:
    if fam.FamilyCategory and fam.FamilyCategory.Id == target_category_id:
        panel_families.append(fam)

# 5. INTERFAZ GRÁFICA DE SELECCIÓN
class FamilySelectionWindow(Window):
    def __init__(self, families):
        self.Title = "Catálogo Metal Perfil: Exportador GLB Corregido"
        self.Width = 500
        self.Height = 500
        self.WindowStartupLocation = WindowStartupLocation.CenterScreen
        self.selected_families = []
        self.ui_map = {}
        
        main_panel = StackPanel()
        self.listbox = ListBox()
        self.listbox.Height = 400
        
        for fam in families:
            cb = CheckBox()
            cb.Content = fam.Name
            self.listbox.Items.Add(cb)
            self.ui_map[cb] = fam
            
        main_panel.Children.Add(self.listbox)
        btn = Button()
        btn.Content = "Generar y Reparar Archivos GLB"
        btn.Height = 40
        btn.Click += self.on_accept
        main_panel.Children.Add(btn)
        self.Content = main_panel

    def on_accept(self, sender, args):
        for cb in self.listbox.Items:
            if cb.IsChecked:
                self.selected_families.append(self.ui_map[cb])
        self.DialogResult = True
        self.Close()

# 6. EJECUCIÓN PRINCIPAL
if not panel_families:
    TaskDialog.Show("Aviso", "No se encontraron familias de Curtain Wall Panels cargadas.")
else:
    win = FamilySelectionWindow(panel_families)
    
    if win.ShowDialog() and len(win.selected_families) > 0:
        output_folder = r"D:\Arquitectura\W_TRABAJOS\12_IA_OPT\2601_METALPERFIL_Catalogo\02_PROYECTO\01_Familias de Piezas metálicas\Export_GLB"
        tools_dir = r"D:\Arquitectura\W_TRABAJOS\12_IA_OPT\2601_METALPERFIL_Catalogo\02_PROYECTO\01_Familias de Piezas metálicas\Herramientas"
        
        if not os.path.exists(output_folder):
            os.makedirs(output_folder)
            
        fbx2gltf_exe = None
        if os.path.exists(tools_dir):
            for archivo in os.listdir(tools_dir):
                if archivo.lower().startswith("fbx2gltf") and archivo.lower().endswith(".exe"):
                    fbx2gltf_exe = os.path.join(tools_dir, archivo)
                    break
        
        if not fbx2gltf_exe:
            TaskDialog.Show("Error", "No se encontró el ejecutable 'FBX2glTF.exe' en Herramientas.")
        else:
            success_count = 0
            
            for family in win.selected_families:
                clean_name = "".join([c for c in family.Name if c.isalnum() or c in (' ', '_', '-')]).rstrip()
                
                family_doc = doc.EditFamily(family)
                if family_doc:
                    view_collector = FilteredElementCollector(family_doc).OfClass(View3D)
                    fam_view3d = None
                    for v in view_collector:
                        if not v.IsTemplate:
                            fam_view3d = v
                            break
                    
                    if fam_view3d:
                        view_set = ViewSet()
                        view_set.Insert(fam_view3d)
                        fbx_options = FBXExportOptions()
                        
                        export_success = family_doc.Export(output_folder, clean_name, view_set, fbx_options)
                        family_doc.Close(False)
                        
                        fbx_file = os.path.join(output_folder, clean_name + ".fbx")
                        glb_file = os.path.join(output_folder, clean_name + ".glb")
                        
                        if export_success and os.path.exists(fbx_file):
                            # Ejecutar convertidor externo
                            comando = [fbx2gltf_exe, "-i", fbx_file, "-o", glb_file]
                            subprocess.call(comando)
                            
                            # 🩹 PARCHE CRÍTICO: Limpiar la cámara corrupta de Revit que bloquea el archivo
                            if os.path.exists(glb_file):
                                reparado = limpiar_camaras_gltf(glb_file)
                                if reparado:
                                    success_count += 1
                            
                            # Borrar el FBX temporal
                            try:
                                os.remove(fbx_file)
                            except:
                                pass
                    else:
                        family_doc.Close(False)
            
            TaskDialog.Show("Éxito", "¡Catálogo procesado y reparado!\n\nSe han generado {} archivos GLB listos para abrir en:\n{}".format(success_count, output_folder))