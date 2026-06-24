import ezdxf
import json
import math
import re

def clean_mtext(text):
    if text.startswith('{') and text.endswith('}'):
        text = text[1:-1]
    text = re.sub(r'\\[a-zA-Z0-9|.]+;', '', text)
    text = re.sub(r'\\[a-zA-Z0-9]', ' ', text)
    if ';' in text:
        text = text.split(';')[-1]
    return text.strip()

def get_bounds(vertices):
    min_x = min(v[0] for v in vertices)
    max_x = max(v[0] for v in vertices)
    min_y = min(v[1] for v in vertices)
    max_y = max(v[1] for v in vertices)
    return min_x, max_x, min_y, max_y

def extract_profiles(dxf_file, output_json):
    doc = ezdxf.readfile(dxf_file)
    msp = doc.modelspace()
    
    # 1. Labels from multiple layers
    labels = []
    for entity in msp.query('TEXT MTEXT'):
        if entity.dxf.layer.upper() not in ["ENCABEZADOS", "PERFIL ENCABEZADO", "ANOTACIONES"]:
            continue
            
        text_content = entity.dxf.text if entity.dxftype() == 'TEXT' else entity.text
        clean_text = clean_mtext(text_content)
        if not clean_text or clean_text.lower().startswith('desarrollo') or '=' in clean_text:
            continue
            
        # Match pattern "Family Number"
        match = re.search(r'^([A-Za-z]+)\s*(\d+)?', clean_text)
        if match:
            family = match.group(1)
            type_num = match.group(2) if match.group(2) else ""
            labels.append({
                'family': family,
                'type': type_num,
                'pos': (entity.dxf.insert.x, entity.dxf.insert.y)
            })

    # 2. Polylines from FIBRA NEUTRA and PERFIL ENCABEZADO
    polylines_list = []
    for layer in ["FIBRA NEUTRA", "PERFIL ENCABEZADO"]:
        for pline in msp.query(f'LWPOLYLINE POLYLINE[layer=="{layer}"]'):
            if pline.dxftype() == 'LWPOLYLINE':
                vertices = [(p[0], p[1]) for p in pline.get_points()]
            else:
                vertices = [(p.dxf.location.x, p.dxf.location.y) for p in pline.points()]
            
            if len(vertices) >= 2:
                # Calculate length
                length = sum(math.dist(vertices[i], vertices[i+1]) for i in range(len(vertices)-1))
                if length > 200: 
                    polylines_list.append(vertices)

    profile_data = []
    for vertices in polylines_list:
        min_x, max_x, min_y, max_y = get_bounds(vertices)
        center_y = (min_y + max_y) / 2
        
        best_label = None
        min_dist = float('inf')
        
        for label in labels:
            dx = min_x - label['pos'][0]
            dy = label['pos'][1] - center_y
            dist = math.sqrt(dx**2 + (dy * 3)**2)
            if dist < min_dist:
                min_dist = dist
                best_label = label
        
        if best_label and min_dist < 4000:
            profile_data.append({
                'label': best_label,
                'vertices': vertices
            })

    # Dedup
    unique_profiles = []
    seen_geom = set()
    for p in profile_data:
        v = p['vertices']
        length = sum(math.dist(v[i], v[i+1]) for i in range(len(v)-1))
        sig = (round(length, 0), round(v[0][0], 0), round(v[0][1], 0), round(v[-1][0], 0), round(v[-1][1], 0))
        if sig not in seen_geom:
            seen_geom.add(sig)
            unique_profiles.append(p)
            
    # Group by family
    grouped_results = {}
    for p in unique_profiles:
        label = p['label']
        family = label['family']
        type_val = label['type']
        
        if family not in grouped_results:
            grouped_results[family] = []
            
        grouped_results[family].append({
            "type": type_val,
            "vertices": [[round(v[0], 3), round(v[1], 3)] for v in p['vertices']]
        })

    # Sort each family by type number
    for family in grouped_results:
        grouped_results[family].sort(key=lambda x: int(x['type']) if x['type'].isdigit() else 0)

    with open(output_json, 'w', encoding='utf-8') as f:
        json.dump(grouped_results, f, indent=2, ensure_ascii=False)
    
    print(f"Extracted {len(unique_profiles)} profiles grouped by family into {output_json}")

if __name__ == "__main__":
    extract_profiles("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf", "perfiles_final.json")
