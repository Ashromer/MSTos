# Cómo sustituir las imágenes (carpeta `content/`)

Cada proyecto tiene su carpeta. Para cambiar una imagen, **sustituye el archivo manteniendo el nombre**.

## Estructura
```
content/
  arquitectura/   visualizacion/   tech/
     <proyecto>/
        logo.png        ← logo del proyecto (centro de la barra inferior)
        01.<ext>        ← imagen/vídeo 1 (la que se ve primero)
        02.<ext>        ← imagen/vídeo 2
        03.<ext>        ← ...
```

## Reglas de nombres
- **`logo`** + su extensión (`logo.png`, `logo.jpg`…). Mejor PNG con fondo transparente.
- Las imágenes/vídeos van **numeradas en orden**: `01`, `02`, `03`… Ese es el orden de los círculos.
- Mantén la **misma extensión** al sustituir (si cambias `01.png` por un `.jpg`, avísame para reapuntar el código).
- Vídeos en `.mp4` (recomprimir a <15-20 MB para web).

## Proyectos por sección

**arquitectura/**: waraqa · orkide · lighthouse · barbate · tfm · campillos · puerto-torre · colonizar-fabrica
**visualizacion/**: caixaforum · csic · sem · tec · kaira-looro · waraqa-school · carrer-marroc · barajas · barcelona-housing · monterrey
**tech/**: metalperfil · canopy-ia · cnc-snake-path · gagn-gemelo-digital · dynamo-suite

## Estado del cableado (código → carpeta)
- ✅ `arquitectura/waraqa` y `visualizacion/caixaforum` ya leen de `content/` (plantilla a sangre).
- ⏳ El resto: las carpetas ya están pobladas con el contenido actual como punto de partida;
  según vayas dejando las imágenes correctas, conecto cada bloque a su carpeta.

> Las imágenes actuales son las que había (algunas son páginas del PDF como provisional).
> Sustitúyelas por los **renders limpios** (sin texto) y los **logos correctos** cuando los tengas.
