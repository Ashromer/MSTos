import ezdxf
import json
import math
import re

def clean_mtext(text):
    text = re.sub(r'\\A[0-9];', '', text)
    text = re.sub(r'\\pt[0-9]+;', '', text)
    while '{' in text:
        start = text.find('{')
        end = text.find('}', start)
        if end == -1: break
        inner = text[start+1:end]
        if ';' in inner: inner = inner.split(';')[-1]
        text = text[:start] + inner + text[end+1:]
    text = re.sub(r'\\[a-zA-Z0-9|.]+;', '', text)
    text = re.sub(r'\\[a-zA-Z0-9]', ' ', text)
    text = text.replace('\\P', ' ').replace('\\X', ' ')
    return text.replace('}', '').replace('{', '').strip()

def extract_for_revit(dxf_file, output_json):
    doc = ezdxf.readfile(dxf_file)
    msp = doc.modelspace()
    
    labels = []
    for entity in msp.query('TEXT MTEXT[layer=="TITULOS"]'):
        text_content = entity.dxf.text if entity.dxftype() == 'TEXT' else entity.text
        clean_text = clean_mtext(text_content)
        
        # Robust Revit parsing: "Family Type/Number [Noise]"
        # Example: "AcerOnda 18 Cassette AC18"
        # We find the first word (Family) and then the next word (Type)
        parts = clean_text.split()
        if len(parts) >= 1:
            family = parts[0].strip()
            # Most profiles have a number as the second part
            # Some might have "24 A"
            type_val = ""
            if len(parts) > 1:
                # If second part is a number, take it and maybe the third if it's a suffix
                if re.match(r'\d+', parts[1]):
                    type_val = parts[1]
                    if len(parts) > 2 and len(parts[2]) == 1: # Suffix A, B, C
                        type_val += " " + parts[2]
                else:
                    # If not a number, just take the second word (e.g., Kubo 17.17.17)
                    type_val = parts[1]
            
            # Remove noise like "Cassette" or dot-segments
            type_val = re.sub(r'Cassette.*', '', type_val, flags=re.I).strip()
            
            labels.append({
                'family': family,
                'type': type_val if type_val else "Unique",
                'pos': (entity.dxf.insert.x, entity.dxf.insert.y)
            })

    raw_polylines = []
    for layer in ["FIBRA NEUTRA", "PERFIL ENCABEZADO"]:
        for pline in msp.query(f'LWPOLYLINE POLYLINE[layer=="{layer}"]'):
            vertices = [(round(p[0], 2), round(p[1], 2)) for p in (pline.get_points() if pline.dxftype() == 'LWPOLYLINE' else [p.dxf.location for p in pline.points()])]
            length = sum(math.dist(vertices[i], vertices[i+1]) for i in range(len(vertices)-1))
            if length > 200:
                raw_polylines.append({'vertices': vertices, 'length': length, 'layer': layer})

    unique_polylines = []
    seen_geom = []
    for pl in raw_polylines:
        is_dup = False
        for seen in seen_geom:
            if abs(pl['length'] - seen['length']) < 1.0 and math.dist(pl['vertices'][0], seen['vertices'][0]) < 10.0:
                is_dup = True; break
        if not is_dup:
            unique_polylines.append(pl); seen_geom.append(pl)

    final_grouped = {}
    for pl in unique_polylines:
        vertices = pl['vertices']
        min_x = min(v[0] for v in vertices)
        center_y = (min(v[1] for v in vertices) + max(v[1] for v in vertices)) / 2
        
        best_label = None
        min_dist = float('inf')
        for label in labels:
            dx = min_x - label['pos'][0]
            dy = label['pos'][1] - center_y
            if dx > -100:
                # Weigh vertical distance more heavily
                dist = math.sqrt((dx if dx > 0 else abs(dx)*10)**2 + (dy * 5)**2)
                if dist < min_dist: min_dist = dist; best_label = label
        
        if best_label and min_dist < 4000:
            fam = best_label['family']
            if fam not in final_grouped: final_grouped[fam] = []
            final_grouped[fam].append({"type": best_label['type'], "vertices": [[v[0], v[1]] for v in vertices]})

    for fam in list(final_grouped.keys()):
        # Numerical sort
        final_grouped[fam].sort(key=lambda x: (int(re.search(r'(\d+)', x['type']).group(1)) if re.search(r'(\d+)', x['type']) else 0, x['type']))

    with open(output_json, 'w', encoding='utf-8') as f:
        json.dump(final_grouped, f, indent=2, ensure_ascii=False)
    
    print(f"Final Report: {sum(len(v) for v in final_grouped.values())} profiles in {len(final_grouped)} families.")

if __name__ == "__main__":
    extract_for_revit("PERFILES ARQUITECTÓNICOS DE FACHADA.dxf", "perfiles_final.json")
