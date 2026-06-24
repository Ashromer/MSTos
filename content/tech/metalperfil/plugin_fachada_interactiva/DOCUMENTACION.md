# Documentación Técnica: Plugin Fachada Interactiva (Metalperfil)

## 1. Descripción General
El **Plugin Fachada Interactiva** es una herramienta avanzada para Autodesk Revit 2026 diseñada para la generación y personalización en tiempo real de fachadas metálicas. A diferencia de versiones anteriores, este plugin elimina la necesidad de selección manual, utiliza un sistema de detección por parámetros y permite la gestión dinámica de materiales, optimizando el flujo de trabajo para visualización en **Twinmotion**.

## 2. Características Principales
- **Detección Automática:** Escaneo de muros mediante el parámetro `MP_GenerarFachada`.
- **Interfaz Modeless (No Modal):** Permite interactuar con Revit y Twinmotion sin cerrar la ventana del plugin.
- **Gestor de Materiales:** 
    - **Global:** Aplica un material único a toda la composición.
    - **Por Tipo:** Asigna materiales específicos a cada familia de panel de forma independiente.
- **Aleatorización Ponderada:** Control preciso de la distribución mediante porcentajes (%).
- **Consola en Vivo:** Feedback detallado del proceso de generación y errores en tiempo real.

## 3. Requisitos del Sistema
- **Software:** Autodesk Revit 2026.
- **Plataforma:** .NET 8.0 (Windows x64).
- **Parámetros:** Requiere la existencia del parámetro de proyecto `MP_GenerarFachada` (Tipo: Sí/No, Categoría: Muros).

## 4. Guía de Uso

### Paso 1: Configuración en Revit
1. Cree un parámetro de proyecto llamado `MP_GenerarFachada`.
2. Asígnelo a la categoría **Muros** como parámetro de instancia.
3. En el modelo, seleccione los muros cortina que desea procesar y active el check `MP_GenerarFachada`.

### Paso 2: Ejecución del Plugin
1. Acceda a la pestaña **Metalperfil** en la cinta de opciones (Ribbon).
2. Haga clic en **Fachada Interactiva**.

### Paso 3: Configuración de la Fachada
1. Pulse **ESCANEAR PROYECTO** para localizar los muros marcados.
2. En la tabla de paneles:
    - Asigne un **Peso (%)** a los paneles que desee incluir (la suma debe ser 100%).
    - Seleccione un **Material Específico** para cada tipo si no desea usar el global.
3. (Opcional) Active **Material Global** y seleccione uno de la lista desplegable para unificar el acabado.

### Paso 4: Aplicación y Sincronización
1. Pulse **APLICAR CAMBIOS (REAL-TIME)**.
2. El plugin regenerará las rejillas y reasignará los paneles y materiales.
3. Si tiene Twinmotion abierto con Direct Link, los cambios se reflejarán automáticamente.

## 5. Estructura de Archivos (Arquitectura)
```text
Plugin Fachada Interactiva/
├── PluginFachadaInteractiva.addin  # Archivo de registro para Revit
└── src/
    ├── Core/
    │   └── App.cs                  # Punto de entrada y creación de UI Ribbon
    ├── Commands/
    │   └── MainCommand.cs          # Lógica de ExternalEvent y Handlers
    ├── Logic/
    │   ├── WallScanner.cs          # Filtrado de muros por parámetros
    │   ├── FachadaProcessor.cs     # Motor de generación y asignación de materiales
    │   ├── PanelSequenceGenerator.cs # Algoritmo de distribución aleatoria
    │   └── FamilyWidthReader.cs    # Extracción de anchos reales de perfiles
    └── UI/
        ├── MainWindow.xaml         # Interfaz de usuario avanzada (WPF)
        └── PanelTypeViewModel.cs   # Modelo de datos para la interfaz
```

## 6. Notas Técnicas
- **Tolerancias:** El algoritmo de rejillas respeta la `ShortCurveTolerance` de Revit (aprox. 0.8mm) para evitar errores de generación.
- **Regeneración:** El plugin limpia las rejillas verticales previas antes de aplicar la nueva distribución para asegurar un diseño limpio.
- **Materiales:** El cambio de material se aplica tanto a nivel de instancia (si el parámetro existe) como a nivel de tipo para asegurar la compatibilidad con familias cargables.

---
*Desarrollado para el Catálogo Metalperfil - 2026*
