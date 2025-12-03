using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TarkovClient
{
    public static class Env
    {
        static Env()
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TarkovClient.exe")
            );

            Version = versionInfo.FileVersion;
        }

        // first logs read on app start
        //public static bool InitialLogsRead { get; set; } = true;

        public static string Version = "0.0";

        public static string WebsiteUrl = "https://tarkov-market.com/pilot";

        private static string _gameFolder = null;
        public static string GameFolder
        {
            get
            {
                if (_gameFolder == null)
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(
                        "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\EscapeFromTarkov"
                    );
                    var installPath = key?.GetValue("InstallLocation")?.ToString();
                    key?.Dispose();

                    if (!String.IsNullOrEmpty(installPath))
                    {
                        _gameFolder = installPath;
                    }
                }

                return _gameFolder;
            }
            set { _gameFolder = value; }
        }

        public static string LogsFolder
        {
            get
            {
                return Path.Combine(GameFolder, "Logs");
                ;
            }
        }

        private static string _screenshotsFolder;
        public static string ScreenshotsFolder
        {
            get
            {
                if (_screenshotsFolder == null)
                {
                    _screenshotsFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Escape From Tarkov",
                        "Screenshots"
                    );
                }
                return _screenshotsFolder;
            }
            set { _screenshotsFolder = value; }
        }

        //===================== AppContext Settings ============================

        private static AppSettings _appSettings = null;

        public static void SetSettings(AppSettings settings, bool force = false)
        {
            if (force || !String.IsNullOrEmpty(settings.gameFolder))
            {
                Env.GameFolder = settings.gameFolder ?? null;
            }
            if (force || !String.IsNullOrEmpty(settings.screenshotsFolder))
            {
                Env.ScreenshotsFolder = settings.screenshotsFolder ?? null;
            }

            // Almacenar objeto AppSettings internamente
            _appSettings = settings;
        }

        public static AppSettings GetSettings()
        {
            // Devolver configuración guardada si existe, de lo contrario crear nueva con valores predeterminados
            if (_appSettings != null)
            {
                // Actualizar información de ruta con valores actuales
                _appSettings.gameFolder = Env.GameFolder;
                _appSettings.screenshotsFolder = Env.ScreenshotsFolder;
                return _appSettings;
            }

            // Advertencia si no hay configuración - esto ocurre si no se llama a Settings.Load()
            return new AppSettings()
            {
                gameFolder = Env.GameFolder,
                screenshotsFolder = Env.ScreenshotsFolder,
            };
        }

        public static void ResetSettings()
        {
            AppSettings settings = new AppSettings()
            {
                gameFolder = null,
                screenshotsFolder = null,
                // Restablecer configuración de PiP a valores predeterminados
                pipEnabled = true,
                pipRememberPosition = true,
                normalWidth = 1400,
                normalHeight = 900,
                normalLeft = -1,
                normalTop = -1,
            };
            SetSettings(settings, true);
        }

        //===================== AppContext Settings ============================

        public static void RestartApp()
        {
            Application.Restart();
            Environment.Exit(0);
        }
    }
}
