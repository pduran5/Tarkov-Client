using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace TarkovClient.Utils
{
    /// <summary>
    /// Clase de gestión de teclas rápidas globales del sistema usando Low-Level Keyboard Hook
    /// Funciona incluso en modo de entrada exclusiva del juego.
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        // Constantes de Low-Level Keyboard Hook
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // Win32 API
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId
        );

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Delegado de Keyboard Hook
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Modifier keys
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt key
        private const int VK_SHIFT = 0x10;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private readonly Window _window;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc = HookCallback;

        // Información de tecla rápida registrada
        private string _registeredKeyString;
        private uint _registeredVirtualKey;
        private bool _requiresControl;
        private bool _requiresAlt;
        private bool _requiresShift;
        private bool _requiresWin;
        private Action _registeredAction;

        private static HotkeyManager _instance;

        public HotkeyManager(Window window)
        {
            _window = window;
            _instance = this;
        }

        /// <summary>
        /// Registra una tecla rápida.
        /// </summary>
        /// <param name="keyString">Cadena de tecla (ej: "F11", "Ctrl", "Alt", "T")</param>
        /// <param name="action">Acción a ejecutar cuando se presiona la tecla rápida</param>
        /// <returns>Éxito del registro</returns>
        public bool RegisterHotkey(string keyString, Action action)
        {
            if (string.IsNullOrEmpty(keyString) || action == null)
                return false;

            try
            {
                // Eliminar tecla rápida existente
                UnregisterAllHotkeys();

                // Analizar cadena de tecla
                var (modifiers, virtualKey) = ParseKeyString(keyString);
                if (virtualKey == 0)
                    return false;

                // Guardar información de tecla rápida
                _registeredKeyString = keyString;
                _registeredVirtualKey = virtualKey;
                _requiresControl = (modifiers & 0x0002) != 0;
                _requiresAlt = (modifiers & 0x0001) != 0;
                _requiresShift = (modifiers & 0x0004) != 0;
                _requiresWin = (modifiers & 0x0008) != 0;
                _registeredAction = action;

                // Instalar Low-Level Hook
                return InstallHook();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Instala Low-Level Keyboard Hook.
        /// </summary>
        private bool InstallHook()
        {
            try
            {
                using (var curProcess = Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    _hookID = SetWindowsHookEx(
                        WH_KEYBOARD_LL,
                        _proc,
                        GetModuleHandle(curModule.ModuleName),
                        0
                    );
                }

                bool success = _hookID != IntPtr.Zero;
                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Función de devolución de llamada de Low-Level Keyboard Hook
        /// </summary>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && _instance != null)
                {
                    // Procesar solo eventos de tecla presionada
                    if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                    {
                        // Extraer código de tecla virtual de la estructura de información de entrada de teclado
                        int vkCode = Marshal.ReadInt32(lParam);

                        if (_instance.IsRegisteredHotkey((uint)vkCode))
                        {
                            // Ejecutar acción en el hilo UI
                            _instance._window?.Dispatcher.BeginInvoke(_instance._registeredAction);

                            // Consumir evento de tecla (no pasar al juego)
                            return (IntPtr)1;
                        }
                    }
                }
            }
            catch (Exception) { }

            // Si no es una tecla rápida registrada, pasar al siguiente Hook
            return CallNextHookEx(_instance?._hookID ?? IntPtr.Zero, nCode, wParam, lParam);
        }

        /// <summary>
        /// Verifica si la entrada de tecla actual es una tecla rápida registrada.
        /// </summary>
        private bool IsRegisteredHotkey(uint vkCode)
        {
            if (_registeredVirtualKey == 0 || vkCode != _registeredVirtualKey)
                return false;

            // Verificar estado de teclas modificadoras
            if (_requiresControl && !IsKeyPressed(VK_CONTROL))
                return false;

            if (_requiresAlt && !IsKeyPressed(VK_MENU))
                return false;

            if (_requiresShift && !IsKeyPressed(VK_SHIFT))
                return false;

            if (_requiresWin && !IsKeyPressed(VK_LWIN) && !IsKeyPressed(VK_RWIN))
                return false;

            // Si se presiona un modificador no requerido, devuelve false
            if (!_requiresControl && IsKeyPressed(VK_CONTROL))
                return false;

            if (!_requiresAlt && IsKeyPressed(VK_MENU))
                return false;

            if (!_requiresShift && IsKeyPressed(VK_SHIFT))
                return false;

            if (!_requiresWin && (IsKeyPressed(VK_LWIN) || IsKeyPressed(VK_RWIN)))
                return false;

            return true;
        }

        /// <summary>
        /// Verifica si una tecla específica está presionada actualmente.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private static bool IsKeyPressed(int vkCode)
        {
            return (GetKeyState(vkCode) & 0x8000) != 0;
        }

        /// <summary>
        /// Analiza la cadena de tecla y devuelve modificadores y tecla virtual.
        /// </summary>
        private (uint modifiers, uint virtualKey) ParseKeyString(string keyString)
        {
            uint modifiers = 0;
            string mainKey = keyString;

            // Procesamiento de teclas modificadoras
            if (keyString.Contains("Ctrl+"))
            {
                modifiers |= 0x0002; // MOD_CONTROL
                mainKey = keyString.Replace("Ctrl+", "");
            }
            if (keyString.Contains("Alt+"))
            {
                modifiers |= 0x0001; // MOD_ALT
                mainKey = keyString.Replace("Alt+", "");
            }
            if (keyString.Contains("Shift+"))
            {
                modifiers |= 0x0004; // MOD_SHIFT
                mainKey = keyString.Replace("Shift+", "");
            }
            if (keyString.Contains("Win+"))
            {
                modifiers |= 0x0008; // MOD_WIN
                mainKey = keyString.Replace("Win+", "");
            }

            // Convertir tecla principal a código de tecla virtual
            uint virtualKey = GetVirtualKeyCode(mainKey);

            return (modifiers, virtualKey);
        }

        /// <summary>
        /// Convierte el nombre de la tecla a código de tecla virtual.
        /// </summary>
        private uint GetVirtualKeyCode(string keyName)
        {
            // Teclas F1-F12
            if (keyName.StartsWith("F") && keyName.Length > 1)
            {
                if (
                    int.TryParse(keyName.Substring(1), out int fKeyNum)
                    && fKeyNum >= 1
                    && fKeyNum <= 12
                )
                {
                    return (uint)(0x70 + fKeyNum - 1); // VK_F1 = 0x70
                }
            }

            // Teclas alfabéticas
            if (keyName.Length == 1 && char.IsLetter(keyName[0]))
            {
                return (uint)keyName.ToUpper()[0];
            }

            // Teclas numéricas
            if (keyName.Length == 1 && char.IsDigit(keyName[0]))
            {
                return (uint)keyName[0];
            }

            // Teclas especiales
            return keyName.ToUpper() switch
            {
                "SPACE" => 0x20,
                "ENTER" => 0x0D,
                "ESC" => 0x1B,
                "TAB" => 0x09,
                "BACKSPACE" => 0x08,
                "DELETE" => 0x2E,
                "HOME" => 0x24,
                "END" => 0x23,
                "PAGEUP" => 0x21,
                "PAGEDOWN" => 0x22,
                "UP" => 0x26,
                "DOWN" => 0x28,
                "LEFT" => 0x25,
                "RIGHT" => 0x27,
                _ => 0,
            };
        }

        /// <summary>
        /// Cancela el registro de todas las teclas rápidas.
        /// </summary>
        public void UnregisterAllHotkeys()
        {
            try
            {
                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                    _hookID = IntPtr.Zero;
                }

                _registeredKeyString = null;
                _registeredVirtualKey = 0;
                _requiresControl = false;
                _requiresAlt = false;
                _requiresShift = false;
                _requiresWin = false;
                _registeredAction = null;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Verifica si la cadena de tecla es válida.
        /// </summary>
        public static bool IsValidKeyString(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
                return false;

            try
            {
                var manager = new HotkeyManager(null);
                var (modifiers, virtualKey) = manager.ParseKeyString(keyString);
                return virtualKey != 0;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            UnregisterAllHotkeys();
            _instance = null;
        }
    }
}
