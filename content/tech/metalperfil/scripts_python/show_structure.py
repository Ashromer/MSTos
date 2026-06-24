import json
with open("perfiles_revit.json", "r", encoding="utf-8") as f:
    data = json.load(f)
for family, types in data.items():
    print(f"{family}: {len(types)} tipos")
    for t in types:
        vcount = len(t["vertices"])
        print(f"  - type={t['type']!r}, vertices={vcount}")
