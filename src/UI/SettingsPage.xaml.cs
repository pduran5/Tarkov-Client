using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TarkovClient
{
    /// <summary>
    /// Lógica de interacción para SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : System.Windows.Controls.UserControl
    {
        // Bandera de modo de entrada de teclas rápidas
        private bool _isHotkeyInputMode = false;

        // Mapeo de nombre en español (inglés) -> nombre interno del juego (basado en resultados de pruebas reales)
        private readonly Dictionary<string, string> _mapDisplayToInternal = new Dictionary<
            string,
            string
        >
        {
            ["Ground Zero"] = "sandbox_high_preset",
            ["Factory"] = "factory_day_preset",
            ["Customs"] = "customs_preset",
            ["Woods"] = "woods_preset",
            ["Shoreline"] = "shoreline_preset",
            ["Interchange"] = "shopping_mall",
            ["Reserve"] = "rezerv_base_preset",
            ["The Lab"] = "laboratory_preset",
            ["Lighthouse"] = "lighthouse_preset",
            ["Streets of Tarkov"] = "city_preset",
        };

        // Mapeo inverso de nombre interno del juego -> nombre en español (inglés)
        private readonly Dictionary<string, string> _mapInternalToDisplay;

        private readonly string[] _mapDisplayNames;

        public SettingsPage()
        {
            InitializeComponent();

            // Inicializar diccionario de mapeo inverso
            _mapInternalToDisplay = _mapDisplayToInternal.ToDictionary(
                kvp => kvp.Value,
                kvp => kvp.Key
            );

            // Inicializar arreglo de nombres de visualización
            _mapDisplayNames = _mapDisplayToInternal.Keys.ToArray();
            LoadSettings();
            CreateMapSettingsUI();
            UpdateMapSettingsState(); // Configuración del estado inicial
            UpdateHotkeySettingsState(); // Configuración del estado de teclas rápidas
        }

        /// <summary>
        /// Cargar configuración actual en UI
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var settings = Env.GetSettings();

                // Configuración global de PiP
                GlobalPipEnabledCheckBox.IsChecked = settings.pipEnabled;

                // Configuración de recordar posición de PiP
                PipRememberPositionCheckBox.IsChecked = settings.pipRememberPosition;

                // Configuración de teclas rápidas de PiP
                PipHotkeyEnabledCheckBox.IsChecked = settings.pipHotkeyEnabled;
                PipHotkeyButton.Content = settings.pipHotkeyKey;

                // Configuración de limpieza automática de archivos
                AutoDeleteLogsCheckBox.IsChecked = settings.autoDeleteLogs;
                AutoDeleteScreenshotsCheckBox.IsChecked = settings.autoDeleteScreenshots;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Generación dinámica de UI de configuración por mapa
        /// </summary>
        private void CreateMapSettingsUI()
        {
            try
            {
                var settings = Env.GetSettings();
                MapSettingsPanel.Children.Clear();

                foreach (string mapDisplayName in _mapDisplayNames)
                {
                    // Crear panel de configuración por mapa
                    var mapPanel = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        Margin = new Thickness(0, 5, 0, 5),
                    };

                    // Checkbox de activación de PiP (checkbox primero)
                    var enabledCheckBox = new System.Windows.Controls.CheckBox
                    {
                        Content = mapDisplayName,
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = mapDisplayName,
                    };

                    // Aplicar valor de configuración actual (buscar por nombre interno)
                    string mapInternalName = _mapDisplayToInternal[mapDisplayName];
                    if (
                        settings.mapSettings != null
                        && settings.mapSettings.ContainsKey(mapInternalName)
                    )
                    {
                        enabledCheckBox.IsChecked = settings.mapSettings[mapInternalName].enabled;
                    }
                    else
                    {
                        enabledCheckBox.IsChecked = true; // Valor predeterminado
                    }

                    // Manejador de eventos del checkbox
                    enabledCheckBox.Checked += MapEnabled_Changed;
                    enabledCheckBox.Unchecked += MapEnabled_Changed;

                    // Agregar control al panel (solo checkbox)
                    mapPanel.Children.Add(enabledCheckBox);

                    MapSettingsPanel.Children.Add(mapPanel);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Cambio de configuración de activación global de PiP
        /// </summary>
        private void GlobalPipEnabled_Changed(object sender, RoutedEventArgs e)
        {
            // Actualizar estado de activación/desactivación de configuración por mapa
            UpdateMapSettingsState();
        }

        /// <summary>
        /// Cambio de configuración de activación por mapa
        /// </summary>
        private void MapEnabled_Changed(object sender, RoutedEventArgs e)
        {
            // No guardar en tiempo real, solo guardar con el botón Guardar
        }

        /// <summary>
        /// Cambio de configuración de activación de tecla rápida de PiP
        /// </summary>
        private void PipHotkeyEnabled_Changed(object sender, RoutedEventArgs e)
        {
            UpdateHotkeySettingsState();
        }

        /// <summary>
        /// Actualizar estado de activación/desactivación de configuraciones por mapa y dependientes
        /// </summary>
        private void UpdateMapSettingsState()
        {
            try
            {
                bool isPipEnabled = GlobalPipEnabledCheckBox.IsChecked ?? false;

                // 1. Activar/desactivar configuraciones dependientes
                // Configuración de recordar posición de PiP
                PipRememberPositionCheckBox.IsEnabled = isPipEnabled;
                PipRememberPositionCheckBox.Opacity = isPipEnabled ? 1.0 : 0.5;

                // Configuración de activación de tecla rápida de PiP
                PipHotkeyEnabledCheckBox.IsEnabled = isPipEnabled;
                PipHotkeyEnabledCheckBox.Opacity = isPipEnabled ? 1.0 : 0.5;

                // Si PiP está desactivado, la configuración de teclas rápidas también se desactiva
                if (!isPipEnabled)
                {
                    UpdateHotkeySettingsState(); // Actualizar UI relacionada con teclas rápidas
                }
                else
                {
                    // Si PiP está activado, actualizar según el estado de configuración de teclas rápidas
                    UpdateHotkeySettingsState();
                }

                // 2. Cambiar estado de todos los controles en el panel de configuración por mapa
                foreach (StackPanel mapPanel in MapSettingsPanel.Children.OfType<StackPanel>())
                {
                    // Buscar checkbox
                    var checkBox = mapPanel
                        .Children.OfType<System.Windows.Controls.CheckBox>()
                        .FirstOrDefault();

                    // Activar/desactivar checkbox
                    if (checkBox != null)
                    {
                        checkBox.IsEnabled = isPipEnabled;
                        checkBox.Opacity = isPipEnabled ? 1.0 : 0.5;
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Clic en botón Guardar
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = Env.GetSettings();

                // Guardar configuración global de PiP
                settings.pipEnabled = GlobalPipEnabledCheckBox.IsChecked ?? true;
                settings.pipRememberPosition = PipRememberPositionCheckBox.IsChecked ?? true;
                settings.pipHotkeyEnabled = PipHotkeyEnabledCheckBox.IsChecked ?? false;
                settings.pipHotkeyKey = PipHotkeyButton.Content?.ToString()?.Trim() ?? "F11";

                // Guardar configuración por mapa
                if (settings.mapSettings == null)
                {
                    settings.mapSettings = new Dictionary<string, MapSetting>();
                }

                // Guardar configuración para cada mapa
                foreach (StackPanel mapPanel in MapSettingsPanel.Children.OfType<StackPanel>())
                {
                    var checkBox = mapPanel
                        .Children.OfType<System.Windows.Controls.CheckBox>()
                        .FirstOrDefault();

                    if (checkBox != null)
                    {
                        string mapDisplayName = checkBox.Tag?.ToString();
                        if (
                            !string.IsNullOrEmpty(mapDisplayName)
                            && _mapDisplayToInternal.ContainsKey(mapDisplayName)
                        )
                        {
                            // Convertir nombre de visualización a nombre interno
                            string mapInternalName = _mapDisplayToInternal[mapDisplayName];

                            if (!settings.mapSettings.ContainsKey(mapInternalName))
                            {
                                settings.mapSettings[mapInternalName] = new MapSetting();
                            }

                            var mapSetting = settings.mapSettings[mapInternalName];
                            mapSetting.enabled = checkBox.IsChecked ?? true;
                        }
                    }
                }

                // Guardar configuración de limpieza automática de archivos
                settings.autoDeleteLogs = AutoDeleteLogsCheckBox.IsChecked ?? false;
                settings.autoDeleteScreenshots = AutoDeleteScreenshotsCheckBox.IsChecked ?? false;

                // Guardar configuración
                Env.SetSettings(settings);
                Settings.Save();

                // Si la configuración de teclas rápidas cambió, volver a registrar en MainWindow
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateHotkeySettings();
                }

                System.Windows.MessageBox.Show(
                    "Configuración guardada.",
                    "Configuración",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error al guardar la configuración: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Actualizar estado de activación/desactivación de configuración de teclas rápidas
        /// </summary>
        private void UpdateHotkeySettingsState()
        {
            try
            {
                bool isPipEnabled = GlobalPipEnabledCheckBox.IsChecked ?? false;
                bool isHotkeyEnabled =
                    (PipHotkeyEnabledCheckBox.IsChecked ?? false) && isPipEnabled;

                // Establecer estado de activación del botón de tecla rápida
                PipHotkeyButton.IsEnabled = isHotkeyEnabled;
                PipHotkeyButton.Opacity = isHotkeyEnabled ? 1.0 : 0.5;

                // Ajustar opacidad del texto de guía
                foreach (var child in HotkeyInputPanel.Children)
                {
                    if (child is TextBlock textBlock)
                    {
                        textBlock.Opacity = isHotkeyEnabled ? 1.0 : 0.5;
                    }
                    else if (child is StackPanel stackPanel)
                    {
                        foreach (var innerChild in stackPanel.Children.OfType<TextBlock>())
                        {
                            innerChild.Opacity = isHotkeyEnabled ? 1.0 : 0.5;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Al hacer clic en el botón de tecla rápida (iniciar modo de entrada)
        /// </summary>
        private void PipHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PipHotkeyButton.Content = "Presione una tecla...";
                PipHotkeyButton.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x6A, 0x6A, 0x2A)
                ); // Luz amarilla

                // Establecer y verificar foco
                bool focusResult = PipHotkeyButton.Focus();

                _isHotkeyInputMode = true;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Al perder el foco del botón de tecla rápida (terminar modo de entrada)
        /// </summary>
        private void PipHotkeyButton_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                // Terminar modo de entrada
                _isHotkeyInputMode = false;

                // Restaurar estado original
                PipHotkeyButton.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A)
                );

                // Si está vacío o es texto de guía, establecer valor predeterminado
                if (
                    PipHotkeyButton.Content?.ToString() == "Pulsa una tecla..."
                    || string.IsNullOrWhiteSpace(PipHotkeyButton.Content?.ToString())
                )
                {
                    PipHotkeyButton.Content = "F11";
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Evento PreviewKeyDown del botón de tecla rápida (capturar todas las teclas)
        /// </summary>
        private void PipHotkeyButton_PreviewKeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e
        )
        {
            try
            {
                if (!_isHotkeyInputMode)
                {
                    return;
                }

                // Permitir tecla Tab para mover el foco
                if (e.Key == System.Windows.Input.Key.Tab)
                {
                    return;
                }

                // Analizar tecla única y establecer inmediatamente
                string keyString = e.Key.ToString();

                if (!string.IsNullOrEmpty(keyString))
                {
                    PipHotkeyButton.Content = keyString;

                    // Terminar modo de entrada y liberar foco
                    _isHotkeyInputMode = false;
                    PipHotkeyButton.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A)
                    );
                    PipHotkeyButton.MoveFocus(
                        new System.Windows.Input.TraversalRequest(
                            System.Windows.Input.FocusNavigationDirection.Next
                        )
                    );
                }

                // Bloquear entrada de tecla
                e.Handled = true;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Evento KeyDown del botón de tecla rápida (procesamiento real de teclas)
        /// </summary>
        private void PipHotkeyButton_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (!_isHotkeyInputMode)
                {
                    return;
                }

                // Ignorar tecla Tab para mover el foco
                if (e.Key == System.Windows.Input.Key.Tab)
                {
                    return;
                }

                string keyString = GetKeyString(e.Key, e.KeyboardDevice.Modifiers);

                if (!string.IsNullOrEmpty(keyString))
                {
                    PipHotkeyButton.Content = keyString;

                    // Liberar foco después de la entrada de tecla para terminar el modo de entrada
                    PipHotkeyButton.MoveFocus(
                        new System.Windows.Input.TraversalRequest(
                            System.Windows.Input.FocusNavigationDirection.Next
                        )
                    );
                }

                e.Handled = true;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Convertir tecla y modificadores a cadena
        /// </summary>
        private string GetKeyString(
            System.Windows.Input.Key key,
            System.Windows.Input.ModifierKeys modifiers
        )
        {
            try
            {
                var keyParts = new List<string>();

                // Agregar teclas modificadoras
                if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                    keyParts.Add("Ctrl");
                if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                    keyParts.Add("Alt");
                if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                    keyParts.Add("Shift");
                if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows))
                    keyParts.Add("Win");

                // Agregar tecla principal
                string mainKey = GetMainKeyString(key);
                if (!string.IsNullOrEmpty(mainKey))
                {
                    keyParts.Add(mainKey);
                    return string.Join("+", keyParts);
                }

                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Convertir tecla principal a cadena
        /// </summary>
        private string GetMainKeyString(System.Windows.Input.Key key)
        {
            // Teclas F
            if (key >= System.Windows.Input.Key.F1 && key <= System.Windows.Input.Key.F12)
                return key.ToString();

            // Teclas numéricas
            if (key >= System.Windows.Input.Key.D0 && key <= System.Windows.Input.Key.D9)
                return key.ToString().Replace("D", "");

            // Teclas alfabéticas
            if (key >= System.Windows.Input.Key.A && key <= System.Windows.Input.Key.Z)
                return key.ToString();

            // Otras teclas especiales
            switch (key)
            {
                case System.Windows.Input.Key.Space:
                    return "Space";
                case System.Windows.Input.Key.Enter:
                    return "Enter";
                case System.Windows.Input.Key.Escape:
                    return "Esc";
                case System.Windows.Input.Key.Back:
                    return "Backspace";
                case System.Windows.Input.Key.Delete:
                    return "Delete";
                case System.Windows.Input.Key.Home:
                    return "Home";
                case System.Windows.Input.Key.End:
                    return "End";
                case System.Windows.Input.Key.PageUp:
                    return "PageUp";
                case System.Windows.Input.Key.PageDown:
                    return "PageDown";
                case System.Windows.Input.Key.Up:
                    return "Up";
                case System.Windows.Input.Key.Down:
                    return "Down";
                case System.Windows.Input.Key.Left:
                    return "Left";
                case System.Windows.Input.Key.Right:
                    return "Right";
                case System.Windows.Input.Key.Insert:
                    return "Insert";
                default:
                    return string.Empty;
            }
        }
    }
}
