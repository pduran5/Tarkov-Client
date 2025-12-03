using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TarkovClient
{
    public static class GameSessionCleaner
    {

        /// <summary>
        /// Limpiar carpetas de registros antiguas al iniciar el programa (excluyendo la carpeta más reciente)
        /// </summary>
        public static void CleanOldLogFolders()
        {
            try
            {
                // Verificar configuración - Salir si la eliminación automática está desactivada
                if (!Env.GetSettings().autoDeleteLogs)
                {
                    return;
                }

                if (!Directory.Exists(Env.LogsFolder))
                {
                    return;
                }

                // Obtener todas las carpetas de registros
                var logDirectories = Directory
                    .GetDirectories(Env.LogsFolder)
                    .OrderByDescending(dir => Directory.GetCreationTime(dir))
                    .ToArray();

                // Limpiar solo si hay al menos 2 carpetas (conservar la más reciente)
                if (logDirectories.Length <= 1)
                {
                    return;
                }

                // Eliminar el resto excepto la carpeta más reciente
                for (int i = 1; i < logDirectories.Length; i++)
                {
                    try
                    {
                        var oldLogDir = logDirectories[i];

                        // Intentar eliminar todos los archivos en la carpeta
                        var files = Directory.GetFiles(
                            oldLogDir,
                            "*.*",
                            SearchOption.AllDirectories
                        );
                        foreach (var file in files)
                        {
                            try
                            {
                                File.SetAttributes(file, FileAttributes.Normal);
                                File.Delete(file);
                            }
                            catch (Exception)
                            {
                                // Ignorar fallo al eliminar archivo individual
                            }
                        }

                        // Intentar eliminar carpeta
                        Directory.Delete(oldLogDir, true);
                    }
                    catch (Exception)
                    {
                        // Ignorar fallo al eliminar carpeta
                    }
                }
            }
            catch (Exception)
            {
                // Ignorar fallo del proceso completo
            }
        }

        /// <summary>
        /// Limpiar archivos de capturas de pantalla (mantener como método independiente)
        /// </summary>

        public static void CleanScreenshotFiles()
        {
            try
            {
                // Verificar configuración - Salir si la eliminación automática está desactivada
                if (!Env.GetSettings().autoDeleteScreenshots)
                {
                    return;
                }

                if (!Directory.Exists(Env.ScreenshotsFolder))
                {
                    return;
                }

                var screenshotFiles = Directory
                    .GetFiles(Env.ScreenshotsFolder, "*.*")
                    .Where(file =>
                    {
                        var ext = Path.GetExtension(file).ToLower();
                        return (ext == ".png" || ext == ".jpg" || ext == ".jpeg");
                    })
                    .ToArray();

                // Optimización de rendimiento con procesamiento paralelo
                Parallel.ForEach(screenshotFiles, screenshotFile =>
                {
                    try
                    {
                        // Intentar cambiar atributos de archivo (desactivar solo lectura)
                        File.SetAttributes(screenshotFile, FileAttributes.Normal);

                        // Intentar eliminación forzada
                        File.Delete(screenshotFile);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                    catch (Exception) { }
                });
            }
            catch (Exception)
            {
                // Ignorar error
            }
        }

    }
}
