# Documentación de fuentes y plan de rebuild — C_CV_Web

> Generado tras búsqueda exhaustiva en `D:\Arquitectura\W_TRABAJOS` (foco en `C_CV\2025`).
> Objetivo: rehacer **Architecture** y **Visualization** con el estilo del `Portfolio_Imagery_MIIM.pdf`.

---

## 1. Plantilla objetivo (estructura por proyecto/imagen)

Replicar el layout del PDF Imagery en HTML responsive:

```
┌───────────────────────────────────────────────────────────┐
│  IMAGEN A SANGRE (de esquina a esquina, full-screen)        │
│                                                             │
│  ┌─ texto izq ─┐                  [LOGO centrado]           │
│  │ Título      │                                           │
│  │ Descripción │                          ① ② ③ ④  ← círculos │
│  └─────────────┘                             nº de imagen   │
└───────────────────────────────────────────────────────────┘
```

- Imagen de fondo `object-fit: cover`, 100vw × 100vh.
- Texto superpuesto a la izquierda (título + descripción, estilo Imagery).
- Logo del proyecto centrado (abajo-centro).
- Círculos numerados como navegación entre imágenes del proyecto.

### Corrección "logos primero" (`Captura_Correccion.JPG`)
En la **intro de cada sección** (`tab-intro-block`), añadir una **fila de logos** de
clientes/software antes del scroll al contenido (los 5 recuadros dibujados).

---

## 2. Decisiones confirmadas por el usuario

| Tema | Decisión |
|---|---|
| Fuente imágenes Architecture | **Mezcla**: sueltas de `ENG\Links` donde existan; mantener montajes de página solo si no hay individual |
| Alcance | **Todo el RTF** (incluye vídeos, Gagn, scripts, TFG, enlaces) |
| Mejora IA de imágenes | **Omitir** (lo hace el usuario). Usar `-enhanced` donde ya exista |

---

## 3. Fuentes por sección

### 3.1 ARCHITECTURE — imágenes sueltas
Origen: `D:\Arquitectura\W_TRABAJOS\C_CV\2025\2025_Portfolio - ENG\Links`

| Proyecto (data-project) | Imágenes sueltas disponibles | Logo |
|---|---|---|
| waraqa | `Escuela.jpg`, `3_KailaLooroFotomon.jpg` (rev.), vídeos Waraqa | `Autocad Logo ONG.png` |
| orkide | `4_Interior orquidea.png`, `4_Fotomontaje.jpg`, `4_Seccion.jpg` | `Autocad Logo.png` |
| lighthouse (Kaira) | `3_Render 01.jpg`, `3_Render 02.jpg`, `3_Render 03.jpg`, `3_Render 04.jpg` | `Autocad Logo ONG.png` |
| barbate | `TorreEntera-enhanced.png` | `Logo- CIP ARQUITECTOS.png` |
| tfm (Erosión/Oasis) | `R2_Oasis.jpg`, `R4_Fachada.jpg`, `6_Vista Lago A1.jpeg`, `6_Vista desde monticulo A1.jpg`, `6_Vista debajo A3.jpeg` | `Revit Logo.jpg` |
| campillos | `RenderExterior_Campillos.png` (en assets) | `Revit Logo.jpg` |
| puerto-torre | `PuertoDeLaTorre.jpg` (en assets) | `Logo-Picharchitects-01.png` |
| colonizar-fabrica | serie `Comocolonizarunafabrica_*` (en assets) | `Revit Logo.jpg` |

> Donde no haya suelta de calidad, se mantiene el `MiguelSuarez_Portfolio_2025_Página_XX.png`.

### 3.2 VISUALIZATION — "Imagery tal cual, bien dispuesto"
Origen páginas: `2025_Portfolio - IMAGERY\PNG\Portfolio_Imagery_MIIM_Página_01..39.png`
Origen renders sueltos: `2025_Portfolio - IMAGERY\Links` (CaixaForum, IBERIA, Vista_A/B/C_4k,
KairaLooro, SEM, GR_11903 día/noche…).
Proyectos ya mapeados en la web: caixaforum, csic, sem, tec, kaira-looro, waraqa-school,
carrer-marroc, barajas, barcelona-housing, monterrey.

### 3.3 TECHNOLOGY — scripts y casos (del RTF)
Origen: `D:\Arquitectura\W_TRABAJOS\5_PICHARCHITECTS\1_BIM\DYNAMO`

| Caso web | Script fuente |
|---|---|
| Generación de planos | `GenerarPlanos1_CrearPlanosColocarVistas.dyn`, `GenerarPlanos2_CrearMoscas.dyn`, `GenerarPlanos3_MoverMoscas.dyn` |
| Reconstrucción urbana OSM | `Generar edificios a partir de OSM\BISTZONA.dyn` |
| Snake-path / perforaciones | `Perforaciones.dyn` |
| Fachada generativa IA | `OpenAI DALL-E Demo.dyn`, `OpenAI Language Demo - Single Answer.dyn` |
| Camino sobre topografía | `Hacer camino sobre topografia.dyn` |
| Pérgolas TEC (splines) | `TEC_TODASPERGOLAS3SPLINES.dyn`, `DYNAMOS_SUELTOS\TEC_*` |
| Ejemplos Python / RevitAPI | carpetas `Python_Ejemplos`, `VISUALSTUDIO REVITAPI` |

GitHub: https://github.com/Ashromer · Behance: https://www.behance.net/miguelsurez1

---

## 4. Vídeos localizados (RTF "Buscar vídeos")

| Vídeo | Ruta | Tamaño | Nota |
|---|---|---|---|
| TEC Interior | `0567 TEC FERRER SALAT\TEC_INTERIOR 11-22.mp4` | 645 MB | Ya hay ligero `assets\TEC_Interior.mp4` |
| TEC Exterior | `0567 TEC FERRER SALAT\TEC_video Exterior.mp4` | 92 MB | Ya hay ligero `assets\TEC_Exterior.mp4` |
| TEC (otro) | `0567 TEC FERRER SALAT\TEC_VIDEO.mp4` | ~ | — |
| Gagn UE | `6_Gagn\1_1.mp4` | 25 MB | Gemelo digital compresor |
| Gagn UE editor | `6_Gagn\Gagn_works - Unreal Editor ….mp4` | 16 MB | — |
| CXF Málaga | `assets\Video_CaixaForum_Malaga.mp4` | — | Ya integrado |
| Hospital/SEM | (pendiente localizar fuente) | — | RTF "Video Hospital" |

> ⚠️ Los vídeos >50 MB deben recomprimirse antes de subir a web (objetivo <15-20 MB/vídeo).

---

## 5. Estado actual de la web vs. objetivo (qué está mal)

1. Imagen **no a sangre**: va en slider con márgenes, no de esquina a esquina.
2. Texto **debajo** (caption + panel deslizante), no superpuesto a la izquierda.
3. Logo **no centrado** sobre la imagen (solo en la tira-índice `nav-strip`).
4. Navegación con **puntos**, no **círculos numerados** de página.
5. Architecture usa **montajes de página** (imágenes juntas), no las **sueltas** de `ENG\Links`.
6. **Falta la fila de logos** en la intro de cada sección.
7. Material del RTF **sin integrar**: vídeos TEC/Gagn, scripts Dynamo, TFG, repos.

---

## 6. Plan de implementación (secuencia)

1. **[este doc]** Inventario y mapeo. ✅
2. Copiar a `assets/` las sueltas + logos que falten (nombres limpios sin espacios/acentos).
3. Nueva plantilla `project-section-block` a sangre (HTML + CSS + JS): img cover, texto izq,
   logo centro, círculos numerados.
4. Architecture: una sección por imagen suelta (separadas) por proyecto.
5. Visualization: páginas Imagery con la misma plantilla a sangre.
6. Fila de logos en cada `tab-intro-block`.
7. Fase 2: vídeos (recomprimidos), Gagn, scripts Dynamo en Technology, TFG/enlaces.
