using System.Collections.Generic;

namespace TarkovClient
{
    public class MapSetting
    {
        public bool enabled { get; set; } = true; // Si la función PiP está activa en este mapa
        public string transform { get; set; }
        public double width { get; set; } = 300;
        public double height { get; set; } = 250;
        public double left { get; set; } = -1;
        public double top { get; set; } = -1;
    }

    public class AppSettings
    {
        public string gameFolder { get; set; }
        public string screenshotsFolder { get; set; }

        // Agregar configuración de PiP
        public bool pipEnabled { get; set; } = false; // Activar/Desactivar función PiP
        public bool pipRememberPosition { get; set; } = true; // Si recordar posición
        public bool pipHotkeyEnabled { get; set; } = false; // Si usar botón de activación de PiP
        public string pipHotkeyKey { get; set; } = "F11"; // Tecla rápida personalizada

        // Agregar configuración de modo normal
        public double normalWidth { get; set; } // Ancho de ventana en modo normal
        public double normalHeight { get; set; } // Alto de ventana en modo normal
        public double normalLeft { get; set; } // Posición X de ventana en modo normal (-1: cálculo automático)
        public double normalTop { get; set; } // Posición Y de ventana en modo normal (-1: cálculo automático)

        // Configuración individual por mapa
        public Dictionary<string, MapSetting> mapSettings { get; set; } = new Dictionary<string, MapSetting>();

        // Configuración de restauración automática de PiP
        public bool enableAutoRestore { get; set; } = true; // Activar función de restauración automática de elementos
        public double restoreThresholdWidth { get; set; } = 800; // Ancho umbral de restauración
        public double restoreThresholdHeight { get; set; } = 600; // Alto umbral de restauración

        // Configuración de limpieza automática de archivos
        public bool autoDeleteLogs { get; set; } = false; // Limpieza automática de carpeta de registros
        public bool autoDeleteScreenshots { get; set; } = false; // Limpieza automática de capturas de pantalla

        public override string ToString()
        {
            return $"gameFolder: '{gameFolder}' \nscreenshotsFolder: '{screenshotsFolder}' \npipEnabled: {pipEnabled}";
        }
    }

    public class MapChangeData : WsMessage
    {
        public string map { get; set; }

        public override string ToString()
        {
            return $"{map}";
        }
    }

    public class UpdatePositionData : WsMessage
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public override string ToString()
        {
            return $"x:{x} y:{y} z:{z}";
        }
    }

    public class SendFilenameData : WsMessage
    {
        public string filename { get; set; }

        public override string ToString()
        {
            return $"{filename}";
        }
    }

    public class QuestUpdateData : WsMessage
    {
        public string questId { get; set; }
        public string status { get; set; }

        public override string ToString()
        {
            return $"{questId} {status}";
        }
    }

    public class WsMessage
    {
        public string messageType { get; set; }

        public override string ToString()
        {
            return $"messageType: {messageType}";
        }
    }

    public class ConfigurationData : WsMessage
    {
        public string gameFolder { get; set; }
        public string screenshotsFolder { get; set; }
        public string version { get; set; }

        public override string ToString()
        {
            return $"gameFolder: '{gameFolder}' \nscreenshotsFolder: '{screenshotsFolder}' \nversion: '{version}'";
        }
    }

    public class UpdateSettingsData : AppSettings
    {
        public string messageType { get; set; }
    }
}
