# Plugin Familias RFA — Documentación técnica
**Proyecto:** Generador automático de familias Revit para perfiles Metalperfil  
**Fecha de última actualización:** 2026-06-11  
**Estado:** Funcional ✅

---

## 1. Qué hace el plugin

Lee un archivo DXF con perfiles de fachada (polilíneas en capa `FIBRA NEUTRA` + etiquetas en capa `TITULOS`), presenta una lista con checkboxes, y genera un archivo `.rfa` de Revit por cada perfil seleccionado.

Cada familia generada contiene:
- Una extrusión con el contorno del perfil (grosor 2 mm, altura 3000 mm por defecto)
- Parámetros `Grosor`, `Altura` y `Material del Panel`
- El tipo de familia nombrado con el nombre del perfil (ej. `AcerOnda_18`)
- La cara superior alineada al plano `Top` de la plantilla
- Subcategoría `Paneles de Fachada`

---

## 2. Estructura del proyecto

```
Plugin Familias rfa/
├── src/                          ← Código fuente C# (.NET 8, WPF)
│   ├── PluginFamilias.csproj
│   ├── Commands/
│   │   └── FamilyCommand.cs      ← IExternalCommand: abre la ventana
│   ├── Core/
│   │   ├── App.cs                ← IExternalApplication: registra botón en ribbon
│   │   └── FamilyGenerator.cs   ← Lógica principal de generación de .rfa
│   ├── CAD/
│   │   └── CadExtractor.cs      ← Extracción de polilíneas y etiquetas del DXF
│   └── UI/
│       ├── MainWindow.xaml       ← Ventana WPF
│       └── MainWindow.xaml.cs   ← Code-behind
├── _backup_FUNCIONA_2026-06-11/  ← Copia de seguridad del estado funcional
├── extract_correct.py            ← Script Python de referencia (extracción DXF)
├── create_revit_families.py      ← Script Python de referencia (generación RFA)
├── PERFILES ARQUITECTÓNICOS DE FACHADA.dxf  ← Archivo DXF de trabajo
└── DOCUMENTACION.md              ← Este archivo
```

### Archivo .addin (registro del plugin en Revit)
```
C:\Users\Usuario\AppData\Roaming\Autodesk\Revit\Addins\2026\PluginFamilias.addin
```
- `FullClassName`: `PluginFamilias.Core.App`
- `Assembly`: ruta absoluta al `PluginFamilias.dll` en `src\bin\Debug\...`

---

## 3. Dependencias

| Paquete | Versión | Uso |
|---|---|---|
| `netDxf` | 2023.11.10 | Lectura de archivos DXF |
| `ACadSharp` | 3.6.12 | Lectura de archivos DWG (soporte básico) |
| RevitAPI.dll | Revit 2026 | API de Revit (referencia local, no NuGet) |
| RevitAPIUI.dll | Revit 2026 | API de UI de Revit |

**Ruta de las DLLs de Revit:**  
`C:\Program Files\Autodesk\Revit 2026\`

---

## 4. Cómo compilar y desplegar

```powershell
# 1. Cerrar Revit (el DLL se bloquea si está abierto)
# 2. Compilar
cd "...Plugin Familias rfa\src"
dotnet build

# 3. El DLL generado queda en:
#    src\bin\Debug\net8.0-windows\win-x64\PluginFamilias.dll

# 4. Abrir Revit → la pestaña "Metalperfil" aparece en el ribbon
```

> **Importante:** si Revit está abierto al compilar, el `.dll` en `bin\Debug` no se actualiza (está bloqueado). En ese caso, copiar manualmente el `.dll` desde `obj\Debug\...` a `bin\Debug\...` tras cerrar Revit.

---

## 5. Cómo usar el plugin

1. Abrir Revit 2026
2. Ir a pestaña **Metalperfil** → botón **Generador Familias**
3. En la ventana:
   - **Fila 1**: seleccionar plantilla `.rft` (por defecto: `Metric Curtain Wall Panel.rft`)
   - **Fila 2**: seleccionar carpeta de salida
   - **Cargar CAD**: seleccionar el archivo DXF
   - La lista se rellena con todos los perfiles encontrados (todos marcados por defecto)
   - Desmarcar los que no se quieran generar
   - **GENERAR FAMILIAS**
4. El log muestra `[OK]` o `[ERROR]` por cada perfil
5. Los archivos `.rfa` se guardan en subcarpetas por nombre de familia:
   ```
   CarpetaSalida/
   ├── AcerOnda/
   │   ├── AcerOnda_18.rfa
   │   └── AcerOnda_20.rfa
   ├── Ritmiko/
   │   └── Ritmiko_24 A.rfa
   └── ...
   ```

---

## 6. Lógica de extracción DXF (`CadExtractor.cs`)

El proceso sigue el mismo algoritmo que `extract_correct.py`:

1. **Etiquetas** (capa `TITULOS`): lee entidades `TEXT` y `MTEXT`; limpia los códigos de formato de MTEXT; parsea el patrón `^([A-Za-z]+)\s+(.+)$` para separar `FamilyName` y `TypeName`
2. **Polilíneas** (capa `FIBRA NEUTRA`): lee `LwPolyline` con vértices `(X, Y, Bulge)`
3. **Deduplicación**: elimina polilíneas duplicadas por clave `(nVértices, longitud÷10, minX÷50, minY÷50)`
4. **Asignación greedy**: construye candidatos (etiqueta, polilínea) donde `-300 ≤ poly.MinX − label.X ≤ 2000 mm` y `distancia combinada < 3000 mm`; asigna en orden de distancia creciente, exclusivo para ambos lados

---

## 7. Lógica de generación de familias (`FamilyGenerator.cs`)

El proceso sigue el mismo algoritmo que `create_revit_families.py`:

### Geometría
1. **Normalize**: centra el perfil en el origen `(0, 0)`
2. **OffsetPolyline**: calcula el contorno paralelo a 2 mm usando normales perpendiculares a cada segmento (promediadas en los vértices)
3. **CreateClosedLoop**: construye el bucle cerrado:
   - Camino de ida (lado A): vértices originales con bulge
   - Tapa final: conecta último punto de A con último punto de B
   - Camino de vuelta (lado B): vértices del offset en orden inverso, con `bulge = -bulge_original`
   - Tapa inicial: cierra B con el primer punto de A
4. **Arcos**: fórmula de punto medio desde bulge DXF con signo invertido (corrección empírica para AcerOnda): `mx = (x1+x2)/2 - (-bulge)*(y2-y1)/2`

### Extrusión con reintento
El perfil se construye directamente desde `OffsetPolyline` + `CreateClosedLoop` (sin librería externa de offset).

Si `NewExtrusion` falla, reintenta con grosor reducido: `2 mm → 1 mm → 0.5 mm → 0.1 mm`.  
Cada intento usa `SubTransaction` para que los fallidos se deshagan sin corromper la transacción principal.  
Los archivos generados con grosor reducido llevan el sufijo `_g{grosor}mm` en el nombre.

### Parámetros y asociaciones
- `Grosor` (Length) → valor constante 2 mm
- `Altura` (Length) → `EXTRUSION_END_PARAM` asociado (controla altura de extrusión)
- `Material del Panel` (Material) → `MATERIAL_ID_PARAM` asociado
- `mgr.NewType(TypeName)` → crea el tipo con el nombre del perfil

### Planos de referencia
Los planos `Left`, `Right` y `Top` se mueven **antes** de crear la extrusión para ajustarse a la geometría del perfil.

### Alineación cara superior
Se busca una vista de elevación en el documento de familia y se crea una alineación (`NewAlignment`) entre la cara superior de la extrusión y el plano `Top`. El bloqueo manual del candado en el editor de familias completa el "pinning".

---

## 8. Problemas conocidos y limitaciones

| Problema | Estado | Notas |
|---|---|---|
| Perfiles con geometría muy compleja (Ritmiko 24, Pyramid 68, Asymetric 80, Aqua 33) | **Resuelto** ✅ | Offset manual con normales radiales en arcos + reintento por grosor reducido (SubTransaction) cubre los casos conocidos |
| Alineación cara superior no bloqueada automáticamente | **Parcial** ⚠️ | `NewAlignment` crea la alineación visible pero el candado debe bloquearse manualmente en el editor de familias (API de Revit 2026 no expone `IsLocked`) |
| Soporte DWG | **Básico** ⚠️ | La extracción DWG via ACadSharp funciona pero no ha sido probada extensivamente |
| Altura y grosor fijos | Pendiente | `AlturaMm = 3000` y `GrosorMm = 2.0` están hardcodeados en `FamilyGenerator.cs` |

---

## 9. Scripts Python de referencia

Los scripts en la raíz del proyecto son la **implementación de referencia** que fue portada a C#:

- `extract_correct.py` — extracción del DXF a JSON (`perfiles_extraidos.json`)
- `create_revit_families.py` — generación de `.rfa` desde el JSON (requiere pyRevit o RevitPythonShell)

Estos scripts siguen funcionando de forma independiente si se necesita depurar la geometría.

---

## 10. Historial de cambios relevantes (sesión 2026-06-11)

### Errores de compilación corregidos
- `App.cs`: `panel.AddItem(...) as PushButton` → pattern matching `is not PushButton`
- `App.cs`: `Path.GetDirectoryName()` devuelve `string?` → guarda `if (dir != null)`  
- `MainWindow.xaml.cs`: 5× `MessageBox` ambiguo → `System.Windows.MessageBox.Show`

### Plugin no cargaba en Revit
- `.addin` apuntaba a `PluginFamilias.Core.App` siendo el namespace `PluginFamilias` (y viceversa en distintos momentos)
- Solución: mantener coherencia entre `namespace` en `App.cs` y `FullClassName` en `.addin`

### Plugin cargaba pero no generaba familias
- **Causa raíz**: `FamilyGenerator` buscaba planos "center/centro/nivel" → no existen en `Metric Curtain Wall Panel.rft` → excepción silenciosa
- **Geometría portada desde Python**: `Normalize`, `OffsetPolyline` perpendicular, `CreateClosedLoop`, arcos con fórmula de bulge correcta
- **Añadido**: `mgr.NewType()`, ajuste de planos Left/Right/Top, asociación `EXTRUSION_END_PARAM`, subcategoría
- **DLL desactualizado**: Revit bloqueaba el `.dll` → copiado manualmente desde `obj\` a `bin\`

### Mejoras adicionales
- `CadExtractor`: soporte MTEXT, deduplicación de polilíneas, algoritmo greedy de matching etiqueta↔polilínea
- `FamilyGenerator`: reintento con `SubTransaction` para perfiles auto-intersectantes
- Logging mejorado: muestra `InnerException` en el log de la UI
- Backup del estado funcional en `_backup_FUNCIONA_2026-06-11\`
