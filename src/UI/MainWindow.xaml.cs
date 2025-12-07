using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using TarkovClient.Constants;
using TarkovClient.Utils;

namespace TarkovClient;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Declaración de API Win32 (para arrastrar ventana)
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    private int _tabCounter = 1;
    private readonly Dictionary<TabItem, WebView2> _tabWebViews = new();
    private PipController _pipController;
    private System.Windows.Threading.DispatcherTimer _settingsSaveTimer;
    private TarkovClient.Utils.HotkeyManager _hotkeyManager;

    public MainWindow()
    {
        try
        {
            InitializeComponent();

            // Inicialización después de cargar la ventana
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }
        catch (Exception)
        {
            throw;
        }
    }

    // Actualizar título de la pestaña
    private static void UpdateTabTitle(TabItem tabItem, string title)
    {
        if (!string.IsNullOrEmpty(title))
        {
            // Cambiar "Tarkov Pilot" a "Tarkov Client"
            string displayTitle = title.Replace("Tarkov Pilot", "Tarkov Client");
            tabItem.Header =
                displayTitle.Length > 20 ? displayTitle.Substring(0, 20) + "..." : displayTitle;
        }
    }

    // Agregar indicador de dirección de Tarkov Market Map (por pestaña)
    private static async Task AddDirectionIndicators(WebView2 webView)
    {
        try
        {
            await Task.Delay(2000); // Esperar a que termine la carga de la página
            await webView.CoreWebView2.ExecuteScriptAsync(
                JavaScriptConstants.ADD_DIRECTION_INDICATORS_SCRIPT
            );
        }
        catch (Exception)
        {
            // Manejo de errores
        }
    }

    // Eliminar elementos de UI innecesarios (por pestaña)
    private static async Task RemoveUnwantedElements(WebView2 webView)
    {
        try
        {
            await webView.CoreWebView2.ExecuteScriptAsync(
                JavaScriptConstants.REMOVE_UNWANTED_ELEMENTS_SCRIPT
            );
        }
        catch (Exception)
        {
            // Manejo de errores
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Ocultar forzosamente panel de carga (para depuración)
        LoadingPanel.Visibility = Visibility.Collapsed;

        await InitializeTabs();

        // Inicializar controlador PiP
        _pipController = new PipController(this);

        // Registrar manejador de eventos para guardar configuración al cambiar tamaño/posición de ventana
        this.SizeChanged += MainWindow_SizeChanged;
        this.LocationChanged += MainWindow_LocationChanged;

        // Inicializar administrador de teclas rápidas
        InitializeHotkeyManager();
    }

    // Inicializar sistema de pestañas y crear primera pestaña
    private async Task InitializeTabs()
    {
        try
        {
            // Crear primera pestaña
            await CreateNewTab();

            // Ocultar panel de carga completo
            LoadingPanel.Visibility = Visibility.Collapsed;
        }
        catch (Exception)
        {
            // Mostrar mensaje de error en panel de carga
            LoadingPanel.Visibility = Visibility.Visible;

            var stackPanel = LoadingPanel.Child as System.Windows.Controls.StackPanel;

            if (stackPanel?.Children.Count > 1)
            {
                var errorText = stackPanel.Children[1] as System.Windows.Controls.TextBlock;
                if (errorText != null)
                {
                    errorText.Text = "Fallo al inicializar el sistema de pestañas";
                }
            }
        }
    }

    // Crear nueva pestaña
    private async Task CreateNewTab()
    {
        try
        {
            // Crear nuevo TabItem
            var newTab = new TabItem
            {
                Header = $"Tarkov Client {_tabCounter}",
                Background = System.Windows.Media.Brushes.Transparent,
            };

            // Crear nuevo WebView2
            var webView = new WebView2
            {
                DefaultBackgroundColor = System.Drawing.Color.Transparent, // Establecer fondo transparente
            };

            // Agregar WebView2 a la pestaña
            newTab.Content = webView;

            // Agregar nueva pestaña a TabControl
            TabContainer.Items.Add(newTab);
            _tabWebViews[newTab] = webView;

            // Seleccionar nueva pestaña
            TabContainer.SelectedItem = newTab;

            // Inicializar WebView2
            await InitializeWebView(webView, newTab);

            _tabCounter++;
        }
        catch (Exception)
        {
            // Manejo de errores en nivel superior
        }
    }

    // Inicializar WebView2
    private async Task InitializeWebView(WebView2 webView, TabItem tabItem)
    {
        try
        {
            // Establecer carpeta de datos de WebView2 en carpeta AppData del usuario
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TarkovClient",
                "WebView2"
            );

            var webView2Environment = await CoreWebView2Environment.CreateAsync(
                null,
                userDataFolder
            );
            await webView.EnsureCoreWebView2Async(webView2Environment);

            // Configuración de WebView2
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

            // Agregar configuración de bypass CSP
            webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            webView.CoreWebView2.Settings.IsScriptEnabled = true;

            // Configurar intercepción de recursos para caché de sitio completo (Offline Support)
            webView.CoreWebView2.AddWebResourceRequestedFilter("*tarkov-market.com*", CoreWebView2WebResourceContext.All);
            webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            // Registrar manejadores de eventos
            webView.NavigationCompleted += (sender, e) =>
                WebView_NavigationCompleted(sender, e, tabItem);
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            webView.CoreWebView2.DocumentTitleChanged += (sender, e) =>
                UpdateTabTitle(tabItem, webView.CoreWebView2.DocumentTitle);

            // Cargar página de Tarkov Market Pilot
            string pilotUrl = Env.WebsiteUrl;
            webView.Source = new Uri(pilotUrl);
        }
        catch (Exception)
        {
            // Manejo de errores
        }
    }

    // Clic en botón TC - Crear nueva pestaña
    private void NewTab_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Crear nueva pestaña
            _ = CreateNewTab();
        }
        catch (Exception) { }
    }

    // Clic en botón Configuración
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Verificar si ya existe pestaña de configuración
            TabItem existingSettingsTab = null;
            foreach (TabItem tab in TabContainer.Items)
            {
                if (tab.Header?.ToString() == "Configuración")
                {
                    existingSettingsTab = tab;
                    break;
                }
            }

            if (existingSettingsTab != null)
            {
                // Si ya existe pestaña de configuración, ir a esa pestaña
                TabContainer.SelectedItem = existingSettingsTab;
            }
            else
            {
                // Crear nueva pestaña de configuración
                CreateSettingsTab();
            }
        }
        catch (Exception) { }
    }


    // Evento de finalización de navegación WebView2 por pestaña
    private void WebView_NavigationCompleted(
        object sender,
        CoreWebView2NavigationCompletedEventArgs e,
        TabItem tabItem
    )
    {
        var webView = sender as WebView2;

        if (e.IsSuccess)
        {
            /* Notificar al servidor WebSocket que WebView está listo (solo en la primera pestaña) */
            if (TabContainer.Items.IndexOf(tabItem) == 0 && Server.CanSend)
            {
                Server.SendConfiguration();
            }

            // Eliminar elementos de UI innecesarios y agregar indicadores de dirección
            _ = RemoveUnwantedElements(webView);
            _ = AddDirectionIndicators(webView);
        }
    }

    // Clic en botón cerrar pestaña
    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as System.Windows.Controls.Button;
        var tabItem = button?.Tag as TabItem;

        /* Mantener al menos 1 pestaña */
        if (tabItem != null && TabContainer.Items.Count > 1)
        {
            CloseTab(tabItem);
        }
    }

    // Cerrar pestaña
    private void CloseTab(TabItem tabItem)
    {
        if (_tabWebViews.TryGetValue(tabItem, out var webView))
        {
            // Limpiar WebView2
            webView?.Dispose();
            _tabWebViews.Remove(tabItem);
        }

        // Eliminar pestaña de TabControl
        TabContainer.Items.Remove(tabItem);
    }

    // Recibir mensaje de JavaScript a C#
    private void CoreWebView2_WebMessageReceived(
        object sender,
        CoreWebView2WebMessageReceivedEventArgs e
    )
    {
        try
        {
            string message = e.TryGetWebMessageAsString();

            // Verificar mensaje vacío
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            // Usar Newtonsoft.Json para análisis JSON
            var messageObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(message);

            string messageType = messageObj?.type?.ToString();

            switch (messageType)
            {
                case "pip-drag-start":
                    // Arrastrar ventana usando API Win32 (alternativa a DragMove)
                    try
                    {
                        // Hacer ventana arrastrable usando API Win32
                        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                        ReleaseCapture();
                        SendMessage(hwnd, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    }
                    catch (Exception) { }
                    break;

                case "pip-exit":
                    // Solicitud de salida de PiP desde JavaScript
                    if (_pipController != null)
                    {
                        _pipController.HidePip();
                    }
                    else { }
                    break;

                case "pip-overlay-ready":
                    break;

                case "pip-toggle":
                    // Solicitud de alternar PiP desde JavaScript
                    if (_pipController != null)
                    {
                        _pipController.TogglePip();
                    }

                    break;

                case "save-map-settings":
                    // Solicitud de guardar configuración por mapa desde JavaScript
                    try
                    {
                        string transform = messageObj?.transform?.ToString();

                        if (_pipController != null && !string.IsNullOrEmpty(transform))
                        {
                            var settings = Env.GetSettings();
                            string currentMap = _pipController.GetCurrentMap();

                            if (!string.IsNullOrEmpty(currentMap))
                            {
                                // Inicializar configuración por mapa (si es necesario)
                                if (settings.mapSettings == null)
                                {
                                    settings.mapSettings =
                                        new System.Collections.Generic.Dictionary<
                                            string,
                                            MapSetting
                                        >();
                                }

                                // Si no existe configuración para el mapa, crear con valores predeterminados
                                if (!settings.mapSettings.ContainsKey(currentMap))
                                {
                                    settings.mapSettings[currentMap] = new MapSetting();
                                }

                                // Guardar valor de transformación y tamaño/posición actual de ventana
                                var mapSetting = settings.mapSettings[currentMap];
                                mapSetting.transform = transform;
                                mapSetting.width = this.Width;
                                mapSetting.height = this.Height;
                                mapSetting.left = this.Left;
                                mapSetting.top = this.Top;

                                // Guardar configuración
                                Env.SetSettings(settings);
                                Settings.Save();
                            }
                        }
                    }
                    catch (Exception) { }
                    break;

                default:
                    break;
            }
        }
        catch (Exception) { }
    }

    // Manejador de intercepción de recursos para caché
    private async void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var webView = sender as Microsoft.Web.WebView2.Core.CoreWebView2;

        // Interceptar todo de tarkov-market.com
        if (e.Request.Uri.Contains("tarkov-market.com"))
        {
            var deferral = e.GetDeferral();
            try
            {
                // Llamar a GetCachedResourceAsync que devuelve struct CacheResult
                var result = await MapCacheManager.GetCachedResourceAsync(e.Request.Uri);
                var stream = result.Stream;
                var contentType = result.ContentType;
                
                if (stream != null)
                {
                    e.Response = webView.Environment.CreateWebResourceResponse(
                        stream,
                        200,
                        "OK",
                        $"Content-Type: {contentType}; Access-Control-Allow-Origin: *");
                }
            }
            catch (Exception) { }
            finally
            {
                deferral.Complete();
            }
        }
    }

    // Evento de cambio de tamaño de ventana
    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Si existe temporizador, reiniciar
        ScheduleSettingsSave();
    }

    // Evento de cambio de posición de ventana
    private void MainWindow_LocationChanged(object sender, EventArgs e)
    {
        // Si existe temporizador, reiniciar
        ScheduleSettingsSave();
    }

    // Programar guardado de configuración (evitar duplicados)
    private void ScheduleSettingsSave()
    {
        // Detener temporizador existente
        _settingsSaveTimer?.Stop();

        // Crear nuevo temporizador o reutilizar
        if (_settingsSaveTimer == null)
        {
            _settingsSaveTimer = new System.Windows.Threading.DispatcherTimer();
            _settingsSaveTimer.Interval = TimeSpan.FromMilliseconds(500);
            _settingsSaveTimer.Tick += (s, args) =>
            {
                _settingsSaveTimer.Stop();

                // Guardar configuración por modo
                if (_pipController != null && _pipController.IsActive)
                {
                    // Modo PiP: Guardar configuración por mapa (solo tamaño/posición de ventana, transform se maneja en JavaScript)
                    SavePipModeSettings();
                }
                else
                {
                    // Modo normal: Guardar configuración de modo normal
                    SaveNormalModeSettings();
                }
            };
        }

        // Iniciar temporizador
        _settingsSaveTimer.Start();
    }

    // Guardar configuración de modo PiP (por mapa)
    private void SavePipModeSettings()
    {
        try
        {
            if (_pipController == null)
            {
                return;
            }

            var settings = Env.GetSettings();
            string currentMap = _pipController.GetCurrentMap();

            if (string.IsNullOrEmpty(currentMap))
            {
                return;
            }

            // Inicializar configuración por mapa (si es necesario)
            if (settings.mapSettings == null)
            {
                settings.mapSettings = new System.Collections.Generic.Dictionary<
                    string,
                    MapSetting
                >();
            }

            // Si no existe configuración para el mapa, crear con valores predeterminados
            if (!settings.mapSettings.ContainsKey(currentMap))
            {
                settings.mapSettings[currentMap] = new MapSetting();
            }

            // Guardar tamaño/posición actual de ventana en configuración por mapa (transform se maneja en JavaScript)
            var mapSetting = settings.mapSettings[currentMap];
            mapSetting.width = this.Width;
            mapSetting.height = this.Height;
            mapSetting.left = this.Left;
            mapSetting.top = this.Top;

            // Guardar configuración
            Env.SetSettings(settings);
            Settings.Save();
        }
        catch (Exception) { }
    }

    // Guardar configuración de modo normal
    private void SaveNormalModeSettings()
    {
        try
        {
            var settings = Env.GetSettings();

            // Guardar solo cuando está en modo normal
            if (_pipController == null || !_pipController.IsActive)
            {
                settings.normalLeft = this.Left;
                settings.normalTop = this.Top;
                settings.normalWidth = this.Width;
                settings.normalHeight = this.Height;

                // Calcular relación de aspecto
                double aspectRatio = this.Width / this.Height;
                string aspectRatioFormatted = $"{aspectRatio:F3}";

                // Determinar relación de aspecto común
                string aspectRatioName = GetAspectRatioName(aspectRatio);

                Env.SetSettings(settings);
                Settings.Save();
            }
        }
        catch (Exception) { }
    }

    // Determinar nombre de relación de aspecto
    private string GetAspectRatioName(double aspectRatio)
    {
        // Rango de tolerancia
        const double tolerance = 0.05;

        // Relaciones de aspecto comunes
        if (Math.Abs(aspectRatio - (16.0 / 9.0)) < tolerance)
            return "16:9";
        if (Math.Abs(aspectRatio - (16.0 / 10.0)) < tolerance)
            return "16:10";
        if (Math.Abs(aspectRatio - (4.0 / 3.0)) < tolerance)
            return "4:3";
        if (Math.Abs(aspectRatio - (21.0 / 9.0)) < tolerance)
            return "21:9 (Ultra-wide)";
        if (Math.Abs(aspectRatio - (32.0 / 9.0)) < tolerance)
            return "32:9 (Super Ultra-wide)";
        if (Math.Abs(aspectRatio - (3.0 / 2.0)) < tolerance)
            return "3:2";
        if (Math.Abs(aspectRatio - (5.0 / 4.0)) < tolerance)
            return "5:4";
        if (Math.Abs(aspectRatio - 1.0) < tolerance)
            return "1:1 (Square)";

        // Relación inusual
        return "Custom";
    }

    // Variables relacionadas con arrastre PiP
    private bool _isDragging = false;
    private System.Windows.Point _lastMousePosition;

    // Área de arrastre superior PiP - Mouse Down
    private void PipDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(this);

            // Capturar mouse
            ((Border)sender).CaptureMouse();
        }
        catch (Exception) { }
    }

    // Área de arrastre superior PiP - Mouse Move
    private void PipDragArea_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        try
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point currentPosition = e.GetPosition(this);

                // Calcular distancia de movimiento
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;

                // Actualizar posición de ventana
                this.Left += deltaX;
                this.Top += deltaY;
            }
        }
        catch (Exception) { }
    }

    // Área de arrastre superior PiP - Mouse Up
    private void PipDragArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        try
        {
            _isDragging = false;

            // Liberar captura de mouse
            ((Border)sender).ReleaseMouseCapture();

            // El guardado de posición se maneja automáticamente en JavaScript al salir del hover
        }
        catch (Exception) { }
    }

    // Área de salida inferior PiP - Clic
    private void PipExitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            // Ejecutar desactivación de PiP
            if (_pipController != null)
            {
                _pipController.HidePip();
            }
        }
        catch (Exception) { }
    }

    // Evento de cierre de ventana
    private void MainWindow_Closed(object sender, EventArgs e)
    {
        try
        {
            // Limpiar ventana PiP
            _pipController?.HidePip();

            // Limpiar administrador de teclas rápidas
            _hotkeyManager?.Dispose();

            // Limpiar WebView2 de todas las pestañas
            foreach (var kvp in _tabWebViews)
            {
                var webView = kvp.Value;
                if (webView?.CoreWebView2 != null)
                {
                    webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                }
                webView?.Dispose();
            }
            _tabWebViews.Clear();

            // Cerrar aplicación
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception)
        {
            // Manejo de errores
        }
    }

    // Devolver WebView2 activo actual
    private WebView2 GetActiveWebView()
    {
        try
        {
            var selectedTabItem = this.TabContainer.SelectedItem as System.Windows.Controls.TabItem;
            if (selectedTabItem != null && _tabWebViews.ContainsKey(selectedTabItem))
            {
                return _tabWebViews[selectedTabItem];
            }

            // Devolver primer WebView2 (fallback)
            return _tabWebViews.Values.FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    // Crear pestaña de configuración
    private void CreateSettingsTab()
    {
        try
        {
            // Crear nuevo TabItem
            var settingsTab = new TabItem
            {
                Header = "Configuración",
                Background = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(255, 26, 26, 26)
                ),
                Foreground = new SolidColorBrush(Colors.White),
            };

            // Crear UserControl de página de configuración
            var settingsPage = new SettingsPage();
            settingsTab.Content = settingsPage;

            // Agregar a TabContainer
            TabContainer.Items.Add(settingsTab);
            TabContainer.SelectedItem = settingsTab;
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Inicializa el administrador de teclas rápidas.
    /// </summary>
    private void InitializeHotkeyManager()
    {
        try
        {
            var settings = Env.GetSettings();

            // Salir si la función de teclas rápidas está desactivada
            if (!settings.pipHotkeyEnabled)
            {
                return;
            }

            // Limpiar si existe administrador de teclas rápidas
            _hotkeyManager?.Dispose();

            // Crear nuevo administrador de teclas rápidas
            _hotkeyManager = new TarkovClient.Utils.HotkeyManager(this);

            // Registrar tecla rápida (el nuevo HotkeyManager ejecuta la acción en el hilo UI)
            bool success = _hotkeyManager.RegisterHotkey(
                settings.pipHotkeyKey,
                () =>
                {
                    _pipController?.TogglePipWindowPosition();
                }
            );
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Actualiza la configuración de teclas rápidas (llamado al cambiar configuración)
    /// </summary>
    public void UpdateHotkeySettings()
    {
        InitializeHotkeyManager();
    }
}
