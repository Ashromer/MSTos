# Skill: ArchViz Render Enhancer para Nano Banana 2

## Objetivo
Transformar capturas de pantalla base, renders con ruido o "clay renders" exportados de Twinmotion o Unreal Engine 5 en imágenes fotorrealistas de alta fidelidad, manteniendo la precisión geométrica y la perspectiva del modelo original.

## Configuración del Modelo
- **Model:** nano-banana-2 (Gemini 3.1 Flash Image)
- **Thinking Level:** High / Dynamic (requerido para evaluar la iluminación global y las texturas antes de renderizar)
- **Mode:** Image-to-Image / Estilo guiado por referencia

## Directrices de Procesamiento (JSON Prompting Structure)
Cuando el usuario proporcione una imagen base de Twinmotion/Unreal, debes estructurar la petición al modelo usando el siguiente esquema de control estricto:

1. **Geometry Preservation (Fidelidad Estructural):** 
   - Analizar las líneas de fuga, la volumetría y los límites arquitectónicos de la imagen de entrada. 
   - Queda estrictamente prohibido alterar la escala de los edificios, mover ventanas o cambiar la estructura principal.

2. **Material Mapping (Texturas HD):**
   - Detectar los materiales base (ej. si ve un plano gris liso donde Twinmotion sugiere hormigón, reescribirlo como "Hormigón visto pulido con sutiles imperfecciones táctiles y microtextura").
   - Tratar el vidrio con refracción realista y mapear maderas con vetas de alta definición consistentes con la iluminación.

3. **Atmospheric & Lighting Engine:**
   - Interpretar los puntos de luz de Unreal Engine (Lumen) o Twinmotion y potenciar la iluminación global.
   - Añadir oclusión ambiental realista, reflejos nítidos en superficies mojadas o pulidas, y corrección cromática cinematográfica.

4. **Environment & Scatter (Entorno Realista):**
   - Reemplazar la vegetación base o los fondos genéricos por árboles, plantas y cielos fotorrealistas integrados orgánicamente con la iluminación de la escena.