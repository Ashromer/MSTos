# Documentación del Proceso de Diseño y Desarrollo Web: Miguel Suárez Torres

Esta documentación detalla cronológicamente el análisis, las decisiones de diseño y la implementación de código realizadas para el portafolio monográfico de **Miguel Suárez Torres** en la carpeta local **[C_CV_Web](file:///D:/Arquitectura/W_TRABAJOS/C_CV_Web)**.

---

## 📋 1. Recuperación del Contexto de la Sesión Anterior

Al iniciar la sesión, se detectó el cierre de la terminal previa. Se procedió a escanear el directorio del sistema en busca de bases de datos de sesión:
1. Se localizó el archivo de base de datos SQLite de la sesión anterior: **`baa2dec5-02fb-4dab-ae3a-2daba9f48642.db`** en la ruta de conversaciones.
2. Se programó un script interno en Python para consultar la tabla `steps` de la base de datos y extraer la transcripción de las solicitudes del usuario y las respuestas previas.
3. Se recuperó el mapa completo del espacio de trabajo **[all_interesting_files.txt](file:///C:/Users/Usuario/.gemini/antigravity-cli/scratch/all_interesting_files.txt)**, que contenía 14.825 archivos indexados.

---

## 🏛️ 2. Restricciones de Propiedad Intelectual y Autoría

El usuario estableció una directiva crítica respecto a la clasificación de sus trabajos:
* **Proyectos de Arquitectura:** No se deben mostrar como obras propias los proyectos desarrollados bajo el sello de *Picharchitects* (como CaixaForum Málaga, TEC Collserola, Viviendas Carrer Marroc). Por tanto, la sección de arquitectura se compone exclusivamente de **concursos independientes y proyectos de cooperación**.
* **Proyectos de Visualización (Renders e Infoarquitectura):** Se permite mostrar libremente cualquier render o imagen tridimensional fotorrealista realizada por el autor (incluyendo los de Picharchitects o su estudio independiente *MSTos*).

---

## 📐 3. Dirección de Diseño y Estética (Renzo Piano, High-Tech Minimal & Play-time.es)

Se adoptó una estética inspirada en el arquitecto **Renzo Piano** y el movimiento **High-Tech**, combinando elegancia editorial con rigurosidad técnica y una visualización inmersiva inspirada en **`play-time.es`**:
* **Paleta de Colores (Refinada):** Minimalismo absoluto. Fondo blanco puro (`#ffffff`), superficies en gris claro frío (`#f7f7f7`), líneas divisorias muy delgadas en gris neutro (`#e5e5e5`), textos principales en negro carbón (`#111111`). Se eliminó el acento Terracota para centrar todo el protagonismo en las imágenes.
* **Banner/Cabecera Superior:** De color negro sólido por defecto, enmarcando la marca profesional.
* **Ocultación de Cabecera al Hacer Scroll:** Al desplazarse hacia abajo, la cabecera completa desaparece. Permanece de manera flotante la marca simplificada **MST** con un trazo negro vertical en el lateral izquierdo, y una burbuja circular negra (menú hamburguesa) en el lateral derecho para navegar.
* **Visual-First Stack (play-time.es style):** En lugar de rejillas de selección o lightboxes modales flotantes, las pestañas de *Arquitectura* y *Visualización* se estructuran como un scroll vertical de proyectos a gran escala (100vh). Para respetar la integridad de las imágenes, éstas no se recortan (`object-fit: contain;`) y se disponen sobre un fondo blanco limpio en formato galería.
* **Sin Bandas de Degradado Negro y con Pie en Tres Columnas:** Eliminamos las bandas oscuras de degradado de los pies de imagen. La barra de información inferior se organiza en un sistema de rejilla de extremo a extremo dividida en tres columnas:
  * **Izquierda (Metadata):** Año, categoría y ubicación en tipografía de tamaño modesto.
  * **Centro (Nombre/Logo del Proyecto):** Actúa como disparador interactivo. Al hacer clic sobre el nombre del proyecto centrado, se despliega suavemente el panel de información técnica hacia arriba. El antiguo botón "+info" se ha eliminado.
  * **Derecha (Paginación por Puntos):** Los círculos delineados se alinean en el extremo derecho, rellenándose según el slide activo.
* **Ocultación de Información Técnica:** Los detalles explicativos y las especificaciones técnicas se ocultan inicialmente. Al hacer clic sobre el nombre del proyecto centrado, el panel técnico (`.block-info-panel`) se desliza suavemente hacia arriba desde la parte inferior del bloque, manteniendo al usuario inmerso en la narrativa visual.
* **Pausas Editoriales:** Cada pocos scrolls de proyecto se intercalan pausas editoriales (`.editorial-section-block`) a pantalla completa con imágenes inspiracionales y textos explicativos sobre la filosofía y el método bioclimático y de visualización del autor.

---

## ⚙️ 4. Estructura de Navegación, Internacionalización y Código

El sitio web opera mediante **pestañas de contenido (tabs)** en lugar de un scroll infinito clásico, estructurando la información bajo la marca profesional **MSTos**:

### A. Pantalla de Bienvenida (Selector de Entrada)
Inspirado en la web de `unstudio.com`, el usuario es recibido por una pantalla a pantalla completa dividida en tres grandes paneles verticales sin áreas blancas ni esquemas (renders completos de igual entidad):
1. **01 // SPATIAL: Bioclimatic Architecture** (usa `project_09.png` a color completo)
2. **02 // VISUAL: Immersive Media & Real-Time** (usa `00_RenderExterior.png` a color completo)
3. **03 // COMPUTATIONAL: BIM Systems & Code** (usa `project_17.png` a color completo)

### B. Sistema de Internacionalización (Bilingüe ES / EN)
Se implementa un sistema híbrido limpio en CSS y JS:
* Los elementos contienen bloques etiquetados con `.lang-es` y `.lang-en`.
* En `styles.css` se ocultan los bloques contrarios al idioma activo en el body (`body.lang-es .lang-en { display: none !important; }`).
* En `script.js`, los botones `.lang-btn` alternan la clase del body e indexan la preferencia en el `localStorage` para recordar la elección del usuario al recargar la página.

### C. Mapeo de Proyectos y Páginas en el Código
Los archivos del portafolio se estructuraron de la siguiente manera:

#### 📂 [index.html](file:///D:/Arquitectura/W_TRABAJOS/C_CV_Web/index.html)
* **Pestaña Arquitectura:** Muestra los 5 proyectos clave apilados verticalmente a pantalla completa (100vh) con carruseles horizontales:
  * *Escuela Bioclimática Waraqa* (Mahandougou, Côte d'Ivoire).
  * *Refugios Bioclimáticos Orkide* (B4H Camp, Turquía-Siria).
  * *Faro de Cooperación (Kaira Looro)* (Senegal/África Central).
  * *Torre en Barbate* (Residencial costero, MSTos).
  * *Erosión / Oasis (TFM)* (Vivienda colectiva y termalismo, ETSAM).
* **Pestaña Visualización (Estructura IMAGERY 2025):** Replicación exacta página por página (páginas 4 a 38) del portfolio físico de visualización, incluyendo sus portadas divisorias y filosofía editorial como pausas a pantalla completa:
  1. *CaixaForum Málaga (2025)* (Picharchitects) - Páginas 4-8.
  2. *CSIC Research Center (2021)* (Picharchitects + MADC) - Páginas 9-11.
  3. *Medical Emergency Services HQ (SEM)* (Picharchitects) - Páginas 12-15.
  4. *Pausa Editorial: Philosophy / Art of Space* - Página 16.
  5. *Tennis Empowerment Center (TEC)* (Picharchitects) - Páginas 17-20.
  6. *Pausa Editorial: Light, shadow, texture & color* - Página 21.
  7. *Cooperation Lighthouse* (Senegal) - Páginas 22-23.
  8. *Pausa Editorial: Bridge between realities* - Página 24.
  9. *Waraqa School* (Côte d'Ivoire) - Páginas 25-27.
  10. *Pausa Editorial: Residential Housing* - Página 28.
  11. *Carrer Marroc Housing* (Barcelona) - Páginas 29-31.
  12. *Barajas Housing* (Madrid) - Páginas 32-33.
  13. *Barcelona Housing* (Barcelona) - Páginas 34-35.
  14. *Monterrey Housing* (México) - Páginas 36-38.
* **Pestaña Tecnología (Servicios Expandidos de 12_IA_OPT):** Consola Revit interactiva, 6 tarjetas técnicas y un bloque especial de **Casos de Estudio con Imágenes Fotorrealistas** generadas por IA:
  * *Integración de Catálogo Metalperfil:* Generación automática de familias paramétricas 3D (.rfa) desde planos vectoriales DXF.
  * *Diseño Paramétrico & IA:* Mapeo automático de volumetrías físicas y espesores en Revit mediante APIs de redes neuronales generativas.
  * *Optimización de Punzonado CNC:* Reducción del 38% del recorrido del cabezal CNC aplicando algoritmos Snake-Path.
* **Pestaña Quién Soy (About):** Grid editorial con fotografía de perfil oficial en B/N (`MiguelS_BW.jpg`), biografía cronológica (Celobert, Picharchitects, MSTos), enlaces de perfiles a Behance, LinkedIn e Issuu, y sección aparte de Publicaciones (TFG).
* **Pestaña Contacto:** Ficha de datos directos (Email, Teléfono, Ubicación) y formulario minimalista para envío de propuestas.

#### 🎨 [styles.css](file:///D:/Arquitectura/W_TRABAJOS/C_CV_Web/styles.css)
* Estilos bilingües, selectores de idioma `ES/EN`, maquetación responsive para el panel de Quién Soy y el formulario de contacto con bordes de 1px minimalistas y transiciones activas.

#### ⚙️ [script.js](file:///D:/Arquitectura/W_TRABAJOS/C_CV_Web/script.js)
* **Custom Cursor:** Control de movimiento suavizado y hovers.
* **Tab Navigation:** Gestión de menús y paneles activos.
* **BIM Console Simulator:** Terminal interactiva.
* **Language Switcher:** Clics en `.lang-btn` que cambian el idioma y guardan la preferencia.
* **Carruseles de Bloque:** Paso de imágenes e info paneles táctiles/teclado para cada sección de proyectos.

---

## 🧭 5. Implementación del Índice Visual de Proyectos y Cursor Invertido (blend-mode)

Se han añadido mejoras críticas de accesibilidad y navegación:
1. **Cursor Invertido Automático:** Se ha configurado el puntero personalizado (`.custom-cursor` y `.custom-cursor-follower`) con `mix-blend-mode: difference;` y color blanco puro en `styles.css`. De esta forma, el cursor se vuelve automáticamente blanco al pasar sobre fondos negros u oscuros (facilitando su visualización) y negro al pasar sobre fondos claros, de forma 100% nativa.
2. **Índice de Proyectos en Tira Horizontal/Vertical (Visual Index):** Se ha diseñado y programado una tira/lista (`.projects-nav-strip`) inspirada en el índice físico del dossier de *IMAGERY*. Al entrar a *Arquitectura* y *Visualización*, debajo del texto de introducción, aparece este índice visual con la siguiente cuadrícula en tres columnas:
   * **Izquierda (Metadata):** El número de proyecto y texto descriptivo en tipografía sobria.
   * **Centro (Logo):** El logotipo corporativo o del cliente correspondiente extraído de la carpeta de enlaces/Links de InDesign (`LA-CAIXA-LOGO.png`, `Logo-Picharchitects-01.png`, etc.).
   * **Derecha (Thumbnail):** Una versión reducida a escala (miniatura) del render principal del proyecto extraído de la misma carpeta.
3. **Animación de Desplazamiento Suave (Scroll):** Se ha añadido en `script.js` un controlador para que, al hacer click en cualquier fila del índice, la página realice una transición de scroll animada y fluida (`window.scrollTo({ behavior: 'smooth' })`) hasta el bloque correspondiente del proyecto en el viewport, activando el snap-scrolling.
4. **Verificación Técnica:** Se han copiado y enlazado todos los recursos necesarios de las carpetas locales de InDesign, comprobando mediante pruebas automáticas con Node.js que no existen rutas rotas ni errores de sintaxis en el código.

---

## 📽️ 6. Integración Completa de Nuevos Proyectos, Videos y Recursos (Estructura RTF)

En la última sesión se consolidó la integración de todos los requerimientos pendientes del documento `Estructura Pagina Web.rtf`:

1. **Ampliación de la Sección de Arquitectura:**
   * **Proyecto CAMPILLOS:** Integrado como un proyecto de rehabilitación integral residencial modelado en Revit. Se copió y vinculó el render principal `RenderExterior_Campillos.png`.
   * **Puerto de la Torre Suelo (SUNC-O-PT.3):** Integrado como proyecto de urbanismo y ordenación territorial desarrollado bajo Picharchitects. Se copió y vinculó la ficha gráfica conceptual `PuertoDeLaTorre.jpg`.
   * **TFM Cómo colonizar una fábrica:** Integrado en su totalidad con una galería de **11 diapositivas** que documentan paso a paso el análisis del complejo fabril textil de Viladomiu Vell, fases de demolición y el sistema tridimensional de parcelación cooperativa.

2. **Inserción e Integración de Vídeos Cinemáticos:**
   * Se cargaron y optimizaron vídeos para el portafolio en formatos bilingües.
   * En **Arquitectura (Waraqa):** Se incluyó un carrusel que contiene los vídeos cinemáticos promocionales, recorridos de día y recorridos nocturnos (`Waraqa_Video.mp4`, `WaraqaDia.mp4`, y `WaraqaNoche.mp4`).
   * En **Visualización (CaixaForum):** Se integró la cinemática de alta resolución de CaixaForum Málaga (`Video_CaixaForum_Malaga.mp4`).
   * En **Visualización (TEC):** Se incluyeron vídeos del recorrido interior y exterior del Tennis Empowerment Center (`TEC_Interior.mp4` y `TEC_Exterior.mp4`).
   * En **Visualización (Cooperation Lighthouse / Kaira Looro):** Se integraron las cinemáticas de día y de noche del faro (`KairaLooroDia.mp4` y `KairaLooroNoche.mp4`).

3. **Ampliación de BIM Automation / Casos de Estudio en Tecnología:**
   * **Gagn VR Digital Twin:** Añadido como caso de estudio 4, integrando Unreal Engine 5 con telemetría en tiempo real y scripts de Dynamo para mantenimiento predictivo (proyecto colaborativo España - Noruega). Se incluyeron las capturas técnicas en la galería.
   * **Suite de Automatización Dynamo:** Añadida como caso de estudio 5, ilustrando la biblioteca de scripts de generación procedimental de planos y asoleamiento en Picharchitects. Se generó un gráfico vectorial conceptual interactivo (`tech_dynamo_nodes.jpg`).

4. **Ampliación de Contenidos Académicos y Enlaces:**
   * **Trabajo Fin de Grado (TFG):** Añadido como una nueva sección destacada en la pestaña **Quién Soy**, detallando la tesis académica *El espacio público y las dinámicas de flujo en la manifestación ciudadana* (ETSAM-UPM), acompañada de su portada oficial `TFG_cover.jpg` y el enlace de descarga directa del documento completo en PDF.
   * **Actualizaciones de Enlaces:** El enlace del portafolio de Behance se actualizó a la nueva dirección (`https://www.behance.net/miguelsurez1`) y se integró el acceso directo al perfil de GitHub del desarrollador (`https://github.com/Ashromer`).

5. **Auditoría e Integridad del Código:**
   * Se ejecutó un script en Python que auditó las etiquetas de apertura y cierre HTML para garantizar un DOM 100% libre de errores.
   * Se comprobó la existencia e integridad de todos los archivos de assets y enlaces enlazados en el código, confirmando el perfecto funcionamiento bilingüe (ES/EN) del portafolio.

---

## 🩹 7. Revisión de Consistencia UI y Correcciones de Maquetación (commit `a373fd9`)

Revisión profesional (web / arquitecto / diseñador) en busca de inconsistencias. Cambios aplicados en `index.html` y `styles.css`:

1. **El header fijo tapaba el inicio de Arquitectura y Visualización.** El `<header>` es `position: fixed` (~88px; su propio botón "volver" usa `top: 104px`), pero `.arch-index` solo reservaba `5rem` (80px) arriba → el rótulo de sección quedaba debajo. Subido a `120px`, igual que `.tab-intro-block` (Tecnología, que no sufría). En móvil ya estaba cubierto por la regla `@media (max-width: 768px)` con `100px !important`.
2. **Tecnología no estaba centrada como las otras dos.** `.arch-manifesto` (Arq/Viz) usa `text-align: center; margin: 0 auto`, mientras Tecnología usaba `.intro-content-wrapper` a la izquierda. Centrado: `.tab-intro-block` → `justify-content: center`; `.intro-content-wrapper` → `text-align/align-items: center` + `margin: 0 auto`. (La diferencia estructural de fondo —Tecnología es intro `100vh`, Arq/Viz son manifiesto+logos— se mantiene; solo se centró.)
3. **Separadores de numeración unificados a `//`** en los cinco rótulos de sección (antes mezclaban `—`, `//` y `/`). Convención: `NN // Etiqueta`. (Los `.mv-num` internos de cada proyecto mantienen su propio formato `NN /`, ya coherente entre sí.)
4. **Rótulos de sección ahora bilingües.** Los eyebrows de Arquitectura, Visualización y el `intro-tag` de Tecnología estaban escritos a pelo (sin `lang-es`/`lang-en`) y no cambiaban con el toggle ES/EN; envueltos en sus spans como el resto.
5. **`.intro-scroll-hint` centrado** (`align-items: center`), que en Visualización quedaba pegado a la izquierda dentro del bloque centrado.
6. **Cache-busting:** `styles.css?v=1.0.5 → 1.0.6` para forzar recarga del CSS en GitHub Pages.

**Pendiente documentado (no abordado por decisión del usuario):** igualar el patrón de entrada de las tres secciones de portfolio (Tecnología sigue siendo intro a pantalla completa frente a manifiesto+logos de Arq/Viz).
