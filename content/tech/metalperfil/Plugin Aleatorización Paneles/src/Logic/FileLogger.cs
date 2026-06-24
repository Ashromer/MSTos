using System;
using System.IO;

namespace PluginAleatorizar.Logic
{
    public static class FileLogger
    {
        private const string LogDirectory = @"D:\Arquitectura\W_TRABAJOS\12_IA_OPT\2601_METALPERFIL_Catalogo\02_PROYECTO\01_Familias de Piezas metálicas\Plugin Aleatorización Paneles\logs";

        public static void Log(string message)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string fileName = $"PluginLog_{DateTime.Now:yyyy-MM-dd}.txt";
                string filePath = Path.Combine(LogDirectory, fileName);
                
                string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(filePath, logLine);
            }
            catch (Exception)
            {
                // Silently fail if logging fails to avoid crashing the plugin
            }
        }
    }
}
