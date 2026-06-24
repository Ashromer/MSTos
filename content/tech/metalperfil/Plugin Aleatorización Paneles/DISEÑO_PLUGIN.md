# Plugin: Aleatorización de Paneles Metalperfil

**Proyecto:** METALPERFIL — Catálogo de Familias de Fachada  
**Fecha:** 2026-06-11  
**Estado:** Diseño conceptual — pendiente de implementación  
**Contexto:** Extensión del ribbon "Metalperfil" existente (Tab ya creado por `Plugin Familias rfa`)

---

## 1. Concepto

Las familias de perfiles metálicos (Pyramid, Symmetric, Kaotic, Asymetric, Escaler, AcerOnda…) son todas chapas plegadas que salen de una misma bobina. Al ser de igual longitud de chapa pero diferente número y ángulo de pliegues, cada tipo tiene **un ancho distinto**. Esto es una propiedad geométrica fija: no se puede estirar un Pyramid para que ocupe el mismo espacio que un Symmetric sin cambiar el perfil.

En un muro cortina, los paneles son instancias de esas familias. La idea de este plugin es **aleatorizar la asignación de tipos** dentro de una misma familia, respetando las proporciones que el proyectista quiera dar a cada tipo, y **regenerar las líneas de rejilla** del muro cortina para que cada panel ocupe exactamente el ancho que le corresponde. Sin huecos, sin solapes.

---

## 2. Flujo de usuario previsto

```
1. El usuario abre el panel "Aleatorizar Paneles" desde el ribbon Metalperfil.
2. Carga las familias disponibles (desde carpeta de .rfa o desde el proyecto abierto).
3. Selecciona una familia (p.ej. "Pyramid") y ve sus tipos con sus anchos.
4. Asigna porcentajes a cada tipo (total debe sumar 100%).
5. Selecciona los muros cortina a procesar (por selección directa o por valor de parámetro).
6. Pulsa "Aleatorizar" → el plugin regenera las rejillas y asigna los tipos.
7. El log muestra cuántos paneles de cada tipo se colocaron y el % real conseguido.
```

---

## 3. Análisis técnico

### 3.1 Obtención del ancho de cada tipo de familia

El plugin lee el ancho **directamente de la geometría de la `FamilySymbol` cargada en el proyecto activo**. No depende del JSON externo ni requiere modificar el plugin generador.

La API de Revit permite obtener la geometría de un `FamilySymbol` sin necesidad de colocar ninguna instancia:

```csharp
Options opts = new Options { ComputeReferences = false };
GeometryElement geom = familySymbol.get_Geometry(opts);
BoundingBoxXYZ bbox = geom.GetBoundingBox();
double widthFt = bbox.Max.X - bbox.Min.X;   // en pies internos de Revit
double widthMm = widthFt * 304.8;
```

Esto devuelve el bounding box de la extrusión del perfil en el sistema de coordenadas local de la familia. Como todos los perfiles se generan con el mismo origen y orientación (el plugin generador los normaliza), el ancho resultante es fiable y consistente entre tipos.

**Ventaja**: funciona con cualquier familia ya cargada en el proyecto, independientemente de si tiene o no un parámetro `Ancho` explícito. El dato se lee en tiempo de ejecución, siempre actualizado.

### 3.2 Estructura del muro cortina en la API de Revit

Un `Wall` de tipo cortina expone `wall.CurtainGrid` → `CurtainGrid`.

Sobre el `CurtainGrid`:
- `GetVGridLineIds()` / `GetUGridLineIds()` → IDs de las líneas de rejilla verticales y horizontales.
- `AddGridLine(GridLineOrientation, double, bool)` → añadir línea de rejilla a una posición relativa (0.0 = extremo izq, 1.0 = extremo der para orientación vertical).
- `RemoveSegment(ElementId)` → eliminar un segmento de línea.
- `GetCurtainCells()` → colección de `CurtainCell` (cada celda del grid).
- Cada `CurtainCell` tiene `GetPanelIds()` → ID del panel que la ocupa.
- El panel es una `FamilyInstance`; se puede cambiar su tipo con `panel.ChangeTypeId(newTypeId)`.

Limitación importante: **las líneas de rejilla en Revit no se pueden borrar directamente**; solo se pueden "eliminar segmentos" o bloquear/desbloquear. Para regenerar el grid desde cero hay que:
1. Eliminar todos los segmentos de las `CurtainGridLine` verticales existentes.
2. Crear nuevas líneas a las posiciones calculadas.
3. Asignar tipos a los paneles resultantes.

Alternatively, en algunos contextos se puede operar con `pinned/unpinned` y usar `Document.Delete(gridLineId)` si la línea no está anclada.

### 3.3 Algoritmo de aleatorización con anchos fijos

**Entrada:**
- Tipos disponibles: `[(T1, w1), (T2, w2), ..., (Tn, wn)]`
- Porcentajes deseados: `[p1, p2, ..., pn]` (suman 1.0)
- Ancho total del muro: `W`

**Algoritmo propuesto (Weighted Shuffle Fill):**

```
1. Estimar cuántos paneles entran: N_est = W / ancho_promedio_ponderado
2. Generar lista inicial: para cada tipo i, incluir round(pi * N_est) instancias
3. Barajar con Fisher-Yates
4. Recorrer la lista, acumulando ancho:
   - Si acumulado + w[i] <= W + tolerancia (5mm): añadir panel
   - Si no: buscar en los restantes algún tipo que quepa exactamente
   - Si nada cabe: cerrar con el tipo más estrecho disponible o ajustar la última celda
5. Si queda espacio sobrante < w_min: distribuir proporcionalmente entre los últimos N paneles
   (esto implica modificar el parámetro Ancho de esas instancias, si el perfil lo permite)
```

**Variante simplificada (primera iteración)**: Ignorar el sobrante y dejar que la última celda se ajuste en ancho (muro cortina flexible). Si el panel es una familia paramétrica con el ancho como parámetro de tipo, solo existirá si ese tipo ya tiene ese ancho. Si no es paramétrico, la última celda quedará más estrecha o más ancha que el perfil ideal → se puede notificar como advertencia.

### 3.4 Multi-muro

Si se aplica a varios muros:
- Cada muro tiene su propio ancho → el algoritmo se corre de forma independiente para cada uno.
- La semilla aleatoria puede ser fija (reproducible) o variable por muro.
- Opción de "semilla global" vs "semilla por muro" para controlar si los muros se parecen entre sí.

---

## 4. Retos identificados y soluciones

| Reto | Gravedad | Solución |
|---|---|---|
| Manipulación del CurtainGrid (borrar/crear líneas) | Alta | Usar `Document.Delete` con las GridLines desancladas + `AddGridLine` en posiciones calculadas |
| Ancho del perfil no existe como parámetro en el RFA | Resuelto | Leer el bounding box de la geometría de `FamilySymbol` directamente desde el proyecto |
| El sobrante de ancho al final del muro | Media | Algoritmo de ajuste proporcional o panel de cierre específico |
| Muro cortina con paneles ya asignados (no vacío) | Media | Antes de regenerar, leer los tipos actuales para no destruir trabajo manual |
| Familias no cargadas en el proyecto | Media | Detectar qué familias están en el proyecto vs. en disco; ofrecer carga automática |
| Paneles con orientación no estándar (muros curvos) | Baja (V1) | Excluir de V1; anotar para futura versión |
| Undo/Redo de Revit | Alta | Envolver toda la operación en una única `Transaction` con nombre descriptivo |

---

## 5. Arquitectura del plugin

```
Plugin Aleatorización Paneles/
└── src/
    ├── PluginAleatorizar.csproj
    ├── Core/
    │   └── App.cs                    ← IExternalApplication; añade botón al tab "Metalperfil" existente
    ├── Commands/
    │   └── RandomizerCommand.cs      ← IExternalCommand; abre la ventana
    ├── Logic/
    │   ├── FamilyWidthReader.cs      ← Lee anchos de las familias (del proyecto o JSON)
    │   ├── PanelSequenceGenerator.cs ← Algoritmo de aleatorización (weighted shuffle fill)
    │   └── CurtainWallProcessor.cs   ← Manipula grid + asigna tipos en el muro cortina
    └── UI/
        ├── RandomizerWindow.xaml     ← Interfaz WPF
        └── RandomizerWindow.xaml.cs  ← Code-behind
```

El `App.cs` de este plugin intenta crear el tab "Metalperfil" con `try/catch` (igual que el existente), de modo que si ya existe lo reutiliza. Luego añade el botón en el mismo panel "Automatización" o en uno nuevo "Diseño de Fachada".

---

## 6. Plan de desarrollo (fases)

### Fase 1 — Lectura de familias y UI básica (sin tocar el modelo)
- Ventana con lista de familias cargadas en el proyecto activo.
- Detección de tipos y sus anchos (desde `FamilySymbol` + parámetro `Ancho`).
- UI para asignar porcentajes con validación de suma = 100%.
- Simulación de la secuencia de paneles (log previo sin aplicar).

### Fase 2 — Procesado de un único muro
- Selección de un muro cortina en el modelo.
- Lectura del ancho total del muro.
- Ejecución del algoritmo de aleatorización.
- Regeneración del grid con `AddGridLine` + asignación de tipos.
- Transacción única, con mensaje de éxito/error.

### Fase 3 — Multi-muro y parámetro de filtro
- Filtro por parámetro de instancia (p.ej. `METALPERFIL_Familia = "Pyramid"`).
- Aplicación a todos los muros filtrados.
- Control de semilla aleatoria (fija / por muro / global).

### Fase 4 — Pulido y robustez
- Gestión del sobrante de ancho (panel de ajuste o distribución proporcional).
- Preview en tiempo real de la distribución antes de aplicar.
- Informe de resultados (% real conseguido vs. % deseado).
- Soporte de muro con paneles preexistentes (modo "preservar tipos manuales").

---

## 7. Impresiones y recomendaciones

### Lo que hace que esto sea interesante (y difícil)

El problema central no es la UI ni el cálculo de porcentajes — eso es directo. El problema real es la **matemática de relleno con módulos de ancho variable**. Es esencialmente un problema de empaquetado unidimensional con restricciones de distribución estadística. En la práctica, hay dos escenarios muy diferentes:

**Escenario A — Anchos múltiplos entre sí**: Si, por ejemplo, T1=200mm, T2=300mm y T3=400mm son múltiplos del mínimo común (100mm), siempre se puede construir una combinación que llene exactamente el muro. Es el caso ideal.

**Escenario B — Anchos arbitrarios**: Si los anchos son 217mm, 283mm y 341mm (como ocurre con perfiles reales que dependen de la geometría del pliegue), llenar exactamente un muro de 5000mm con esos módulos es un problema NP-hard en el caso general. En la práctica arquitectónica se acepta una tolerancia (las juntas pueden absorber ±5-10mm) o se acepta que el primer o último panel sea un "panel de ajuste" de tamaño especial.

**Recomendación**: Empezar con la tolerancia como mecanismo de cierre. Si el sobrante es menor que la mitad del panel más estrecho, se distribuye entre los últimos 2-3 paneles. Si es mayor, se busca el tipo cuyo ancho más se acerca al sobrante. Anotar en el log cuánto sobrante hubo.

### Sobre la lectura del ancho

El ancho se obtiene del bounding box de la geometría de la `FamilySymbol` cargada en el proyecto. Esto elimina cualquier dependencia del JSON externo y no requiere tocar el plugin generador. El único requisito es que la familia esté cargada en el proyecto antes de ejecutar el aleatorizado — algo que el flujo natural de trabajo garantiza (primero se generan las familias, luego se aplican).

### Sobre la UI

La ventana del plugin existente es funcional pero mínima. Para este plugin se justifica una UI algo más rica: una tabla editable (ancho fijo) donde cada fila es un tipo con su ancho mostrado y un campo de porcentaje. El total se muestra en tiempo real. Esto se puede hacer bien en WPF con un `DataGrid` simple y una fila de totales.

### Sobre el addin registrado

El nuevo plugin puede ir en un `.addin` separado o compartir el del plugin existente si se refactoriza `App.cs` como una aplicación que registra múltiples comandos. Para V1, un `.addin` independiente es lo más seguro: no toca nada del código existente y se carga en paralelo.

---

## 8. Dependencias con el plugin existente

| Elemento | Dependencia | Acción requerida |
|---|---|---|
| Tab "Metalperfil" en el ribbon | Compartido | El nuevo App.cs hace `try/catch` al crear el tab (ya en el patrón existente) |
| Anchos de perfil | Leídos de `FamilySymbol.get_Geometry()` | Ninguna — funciona con las familias tal como están |
| Familias `.rfa` generadas | Deben estar cargadas en el proyecto | El nuevo plugin detecta si están cargadas y lo notifica en la UI |
| `perfiles_revit.json` | Sin dependencia | No necesario en este plugin |

---

*Documento preparado para validación antes de iniciar implementación.*
