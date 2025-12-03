using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TarkovClient.Utils
{
    /// <summary>
    /// Utilidad robusta para mantener en primer plano usando API de Windows
    /// Proporciona funcionalidad para fijar la ventana en primer plano a nivel del sistema
    /// </summary>
    public static class WindowTopmost
    {
        // Constantes de API de Windows
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWNOACTIVATE = 4;

        // Funciones de API de Windows
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            int hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Forzar fijación de ventana en primer plano a nivel del sistema
        /// </summary>
        /// <param name="window">Ventana WPF de destino</param>
        /// <param name="activate">Si activar la ventana</param>
        /// <returns>Éxito</returns>
        public static bool SetTopmost(Window window, bool activate = true)
        {
            try
            {
                window.Topmost = true;

                var hwnd = GetWindowHandle(window);
                if (hwnd == IntPtr.Zero)
                {
                    return false;
                }

                ShowWindow(hwnd, SW_SHOWNOACTIVATE);

                uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE;
                bool result = SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags);

                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Liberar fijación de ventana en primer plano
        /// </summary>
        /// <param name="window">Ventana WPF de destino</param>
        /// <returns>Éxito</returns>
        public static bool RemoveTopmost(Window window)
        {
            try
            {
                var hwnd = GetWindowHandle(window);
                if (hwnd == IntPtr.Zero)
                    return false;

                bool result = SetWindowPos(
                    hwnd,
                    HWND_NOTOPMOST,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
                );

                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Obtener manejador de ventana (método seguro)
        /// </summary>
        /// <param name="window">Ventana WPF de destino</param>
        /// <returns>Manejador de ventana (IntPtr.Zero si falla)</returns>
        private static IntPtr GetWindowHandle(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;

                if (hwnd == IntPtr.Zero)
                {
                    hwnd = new WindowInteropHelper(window).EnsureHandle();
                }

                return hwnd;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }
    }
}
