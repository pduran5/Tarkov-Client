using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TarkovClient.Utils
{
    /// <summary>
    /// Utilidad de control de transparencia de ventana usando API de Windows
    /// Implementación del método LayeredWindow compatible con WebView2
    /// </summary>
    public static class WindowTransparency
    {
        // Constantes de API de Windows
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_ALPHA = 0x2;

        // Funciones de API de Windows
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd,
            uint colorKey,
            byte alpha,
            uint flags
        );

        /// <summary>
        /// Activar modo transparente para la ventana
        /// </summary>
        /// <param name="window">Ventana WPF de destino</param>
        /// <param name="opacity">Opacidad (0.0 = totalmente transparente, 1.0 = opaco)</param>
        /// <returns>Éxito</returns>
        public static bool EnableTransparency(Window window, double opacity = 1.0)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;

                if (hwnd == IntPtr.Zero)
                {
                    hwnd = new WindowInteropHelper(window).EnsureHandle();
                }

                if (hwnd == IntPtr.Zero)
                {
                    return false;
                }

                // Obtener estilo de ventana actual
                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                // Agregar estilo Layered Window
                var newStyle = extendedStyle | WS_EX_LAYERED;
                var setResult = SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);

                // Configurar opacidad (convertir a rango 0-255)
                byte alpha = (byte)(opacity * 255);
                var transparencyResult = SetLayeredWindowAttributes(hwnd, 0, alpha, LWA_ALPHA);

                // Verificar estilo después de la configuración
                var finalStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                var isLayered = (finalStyle & WS_EX_LAYERED) != 0;

                return transparencyResult;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Actualizar solo la opacidad de la ventana (para ventanas ya configuradas como LayeredWindow)
        /// </summary>
        /// <param name="window">Ventana WPF de destino</param>
        /// <param name="opacity">Opacidad (0.0 = totalmente transparente, 1.0 = opaco)</param>
        /// <returns>Éxito</returns>
        public static bool UpdateTransparency(Window window, double opacity)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                    return false;

                // Actualizar solo opacidad (convertir a rango 0-255)
                byte alpha = (byte)(Math.Max(0.0, Math.Min(1.0, opacity)) * 255);
                return SetLayeredWindowAttributes(hwnd, 0, alpha, LWA_ALPHA);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Desactivar modo transparente de la ventana
        /// </summary>
        /// <param name="window">Ventana WPF de destino</param>
        /// <returns>Éxito</returns>
        public static bool DisableTransparency(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                    return false;

                // Obtener estilo de ventana actual
                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                // Eliminar estilo Layered Window
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_LAYERED);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Verificar si la ventana está en modo LayeredWindow
        /// </summary>
        /// <param name="window">Ventana WPF de destino</param>
        /// <returns>Es LayeredWindow</returns>
        public static bool IsLayeredWindow(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                    return false;

                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                return (extendedStyle & WS_EX_LAYERED) != 0;
            }
            catch
            {
                return false;
            }
        }

    }
}
