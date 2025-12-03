using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using TarkovClient.Constants;
using TarkovClient.Utils;

namespace TarkovClient
{
    public class PipController
    {
        private static PipController _instance;
        public static PipController Instance => _instance;

        private MainWindow _mainWindow;
        private bool _isActive = false;
        private string _currentMap = null;

        // Temporizador para mantener en primer plano
        private DispatcherTimer _topmostTimer;
        private readonly object _timerLock = new object();

        // Seguimiento de estado para función de restauración automática
        private bool _elementsHidden = false;
        private double _lastKnownWidth = 0;
        private double _lastKnownHeight = 0;

        // Propiedad pública para seguimiento de estado de PiP
        public bool IsActive => _isActive;

        // Método para permitir acceso externo a la información del mapa actual
        public string GetCurrentMap() => _currentMap;

        /// <summary>
        /// Alterna el modo PiP (desactiva si está activo, activa si está inactivo)
        /// </summary>
        public void TogglePip()
        {
            if (_isActive)
            {
                HidePip();
            }
            else
            {
                ShowPip();
            }
        }

        /// <summary>
        /// Gestiona la posición de la ventana PiP (alternar Primer plano ↔ Minimizar/Fondo)
        /// Funciona solo cuando el modo PiP está activo
        /// </summary>
        public void TogglePipWindowPosition()
        {
            if (!_isActive)
            {
                return;
            }

            try
            {
                if (_mainWindow.WindowState == System.Windows.WindowState.Minimized)
                {
                    // Estado minimizado → Restaurar + Primer plano (sin cambiar foco)
                    {
                        Utils.WindowTopmost.SetTopmost(_mainWindow);
                    }
                }
                else
                {
                    // Estado en primer plano → Minimizar
                    Utils.WindowTopmost.RemoveTopmost(_mainWindow);
                    _mainWindow.WindowState = System.Windows.WindowState.Minimized;
                }
            }
            catch { }
        }

        public PipController(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _instance = this;
        }

        // Llamado al cambiar mapa (1er disparador)
        public void OnMapChanged(string mapName)
        {
            _currentMap = mapName;

            // Llamar a ShowPip solo cuando la función PiP está activa
            var settings = Env.GetSettings();
            if (settings.pipEnabled)
            {
                var mapSetting = GetMapSetting(settings, _currentMap);
                if (mapSetting.enabled)
                {
                    ShowPip();
                }
                else { }
            }
            else { }
        }

        // Llamado al crear captura de pantalla (2do disparador)
        public void OnScreenshotTaken()
        {
            if (_isActive)
            {
                return;
            }

            ShowPip();
        }

        // Mostrar modo PiP (método de cambio de tamaño de ventana principal)
        public async void ShowPip()
        {
            // Obtener información de configuración
            var settings = Env.GetSettings();

            try
            {
                // Guardar e inicializar configuración de modo normal solo en la primera activación
                if (!_isActive)
                {
                    SaveNormalModeSettings();

                    _isActive = true;

                    // Registrar manejador de eventos de cambio de tamaño de ventana
                    RegisterSizeChangedHandler();
                }

                // Cambio automático a la primera pestaña
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var tabContainer =
                            _mainWindow.FindName("TabContainer")
                            as System.Windows.Controls.TabControl;
                        if (tabContainer != null && tabContainer.Items.Count > 0)
                        {
                            tabContainer.SelectedIndex = 0;
                        }
                        else { }
                    }
                    catch { }
                });

                await Task.Delay(500);

                // Eliminar elementos web y escalar mapa (en pantalla completa) - Ejecutar en hilo UI
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ExecuteElementRemovalAndMapScaling();
                });

                await Task.Delay(500);

                // Configuración de estado inicial (elementos comienzan ocultos)
                _elementsHidden = true;

                await ApplyPipModeSettings();

                ApplyPipWindowSettings();
            }
            catch (Exception)
            {
                _isActive = false;
            }
        }

        // Desactivar modo PiP (restaurar modo normal de ventana principal)
        public void HidePip()
        {
            if (!_isActive)
                return;

            try
            {
                _isActive = false;

                // Eliminar manejador de eventos de cambio de tamaño de ventana
                UnregisterSizeChangedHandler();

                // Inicializar estado
                _elementsHidden = false;
                _lastKnownWidth = 0;
                _lastKnownHeight = 0;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // 1. Restaurar tamaño y posición de modo normal
                    var settings = Env.GetSettings();
                    _mainWindow.Width = settings.normalWidth;
                    _mainWindow.Height = settings.normalHeight;

                    if (settings.normalLeft >= 0 && settings.normalTop >= 0)
                    {
                        _mainWindow.Left = settings.normalLeft;
                        _mainWindow.Top = settings.normalTop;
                    }
                    else
                    {
                        _mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }

                    // 2. Restaurar límite de tamaño mínimo
                    _mainWindow.MinWidth = 1000;
                    _mainWindow.MinHeight = 700;

                    // 3. Restaurar barra de título y modo de cambio de tamaño
                    _mainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                    _mainWindow.ResizeMode = ResizeMode.CanResize;

                    // 4. Desactivar primer plano (WPF + Win32 API)
                    _mainWindow.Topmost = false;
                    WindowTopmost.RemoveTopmost(_mainWindow);
                });

                // Restaurar elementos eliminados por JavaScript
                RestorePipJavaScriptActions();

                // Desactivar UI de modo PiP
                RestoreNormalModeSettings();
            }
            catch (Exception) { }
        }

        // Actualizar contenido del mapa
        public void UpdateMapContent(string mapName)
        {
            _currentMap = mapName;
            // El mapa se sincroniza automáticamente en WebView2 (comunicación WebSocket)
        }

        /// <summary>
        /// Registrar manejador de eventos de cambio de tamaño de ventana
        /// </summary>
        private void RegisterSizeChangedHandler()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _mainWindow.SizeChanged += OnWindowSizeChanged;
                });
            }
            catch (Exception) { }
        }

        // Eliminar elementos web y escalar mapa
        private async Task ExecuteElementRemovalAndMapScaling()
        {
            try
            {
                // Obtener WebView2 de la pestaña activa actual (ejecutar en hilo UI)
                Microsoft.Web.WebView2.Wpf.WebView2 activeWebView = null;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    activeWebView = GetActiveWebView();
                });

                if (activeWebView?.CoreWebView2 == null)
                {
                    return;
                }

                // 0. Eliminar superposición PiP existente (prevenir duplicados al cambiar mapa)
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.REMOVE_PIP_OVERLAY_SCRIPT
                );

                // 1. Escalado de mapa (#map) - Aplicar configuración por mapa
                var settings = Env.GetSettings();
                string transformMatrix = GetMapTransform(settings, _currentMap);

                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    $@"
                    try {{
                        var mapElement = document.querySelector('#map');
                        if (mapElement) {{
                            mapElement.style.transformOrigin = '0px 0px 0px';
                            mapElement.style.transform = '{transformMatrix}';
                        }}
                    }} catch {{
                    }}
                "
                );

                // Eliminar panel derecho
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.REMOVE_TARKOV_MARGET_ELEMENT_PANNEL_RIGHT
                );

                // Eliminar panel izquierdo
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.REMOVE_TARKOV_MARGET_ELEMENT_PANNEL_LEFT
                );

                // 3. Ocultar elementos específicos (panel_top)
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.REMOVE_TARKOV_MARGET_ELEMENT_PANNEL_TOP
                );

                // 4. Ocultar elemento header (display: none)
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.REMOVE_TARKOV_MARGET_ELEMENT_HEADER
                );

                // 5. Ocultar elemento footer-wrap
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.REMOVE_TARKOV_MARGET_ELEMENT_FOOTER
                );
            }
            catch (Exception) { }
        }

        // Aplicar configuración de UI de modo PiP
        private async Task ApplyPipModeSettings()
        {
            try
            {
                var settings = Env.GetSettings();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // Obtener WebView2 activo actual
                    var activeWebView = GetActiveWebView();

                    // Ocultar barra lateral de pestañas y expandir TabControl
                    var tabSidebar =
                        _mainWindow.FindName("TabSidebar") as System.Windows.Controls.Border;
                    var tabContainer =
                        _mainWindow.FindName("TabContainer") as System.Windows.Controls.TabControl;

                    if (tabSidebar != null)
                    {
                        tabSidebar.Visibility = Visibility.Collapsed;
                    }

                    // Expandir TabContainer al ancho completo y ocultar área de encabezado
                    if (tabContainer != null)
                    {
                        System.Windows.Controls.Grid.SetColumn(tabContainer, 0);
                        System.Windows.Controls.Grid.SetColumnSpan(tabContainer, 2);

                        // Empujar área de encabezado hacia arriba para ocultar (altura de encabezado aprox. 30px)
                        tabContainer.Margin = new System.Windows.Thickness(0, -30, 0, 0);

                        // Establecer Z-Index más bajo en modo PiP (para que el área de hover esté encima)
                        System.Windows.Controls.Panel.SetZIndex(tabContainer, 10);
                    }

                    // Bajar Z-Index de TabContainer
                    if (tabContainer != null)
                    {
                        System.Windows.Controls.Panel.SetZIndex(tabContainer, 50);
                    }

                    // Crear superposición PiP basada en JavaScript (incluye verificación y reintento)
                    if (activeWebView != null && activeWebView.CoreWebView2 != null)
                    {
                        // Crear superposición
                        await activeWebView.CoreWebView2.ExecuteScriptAsync(
                            JavaScriptConstants.CREATE_PIP_OVERLAY_SCRIPT
                        );

                        await Task.Delay(500);

                        var verificationResult =
                            await activeWebView.CoreWebView2.ExecuteScriptAsync(
                                @"
                            (function() {
                                const overlay = document.getElementById('tarkov-client-pip-overlay');
                                const controlBar = document.getElementById('pip-control-bar');
                                const webArea = document.getElementById('pip-web-area');
                                
                                return JSON.stringify({
                                    overlayExists: !!overlay,
                                    controlBarExists: !!controlBar,
                                    webAreaExists: !!webArea,
                                    overlayVisible: overlay ? overlay.style.display !== 'none' : false
                                });
                            })()
                            "
                            );

                        // Reintentar si falla la verificación
                        var verification = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(
                            verificationResult
                        );
                        if (
                            !verification.overlayExists
                            || !verification.controlBarExists
                            || !verification.webAreaExists
                        )
                        {
                            // Eliminar completamente superposición existente
                            await activeWebView.CoreWebView2.ExecuteScriptAsync(
                                JavaScriptConstants.REMOVE_PIP_OVERLAY_SCRIPT
                            );

                            await Task.Delay(200);

                            // Regenerar
                            await activeWebView.CoreWebView2.ExecuteScriptAsync(
                                JavaScriptConstants.CREATE_PIP_OVERLAY_SCRIPT
                            );
                        }
                    }
                });
            }
            catch (Exception) { }
        }

        // Aplicar configuración de tamaño/posición de ventana de modo PiP
        private void ApplyPipWindowSettings()
        {
            try
            {
                var settings = Env.GetSettings();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // 1. Liberar temporalmente límite de tamaño mínimo
                    _mainWindow.MinWidth = 200;
                    _mainWindow.MinHeight = 150;

                    // 2. Eliminar barra de título y configurar modo de cambio de tamaño
                    _mainWindow.WindowStyle = WindowStyle.None;
                    _mainWindow.ResizeMode = ResizeMode.CanResize;

                    // 3. Configurar tamaño y posición de PiP - Aplicar configuración por mapa
                    var mapSetting = GetMapSetting(settings, _currentMap);
                    _mainWindow.Width = mapSetting.width;
                    _mainWindow.Height = mapSetting.height;

                    // 4. Configurar posición - Aplicar configuración por mapa
                    if (mapSetting.left >= 0 && mapSetting.top >= 0)
                    {
                        _mainWindow.Left = mapSetting.left;
                        _mainWindow.Top = mapSetting.top;
                    }
                    else
                    {
                        // Posición predeterminada: Inferior derecha de la pantalla
                        _mainWindow.Left =
                            SystemParameters.PrimaryScreenWidth - mapSetting.width - 0;
                        _mainWindow.Top =
                            SystemParameters.PrimaryScreenHeight - mapSetting.height - 80;
                    }

                    // 5. Aplicar configuración de primer plano (unificado con el mismo método que la tecla rápida)
                    bool topmostResult = WindowTopmost.SetTopmost(_mainWindow);
                });
            }
            catch (Exception) { }
        }

        // Restaurar elementos eliminados por JavaScript al desactivar modo PiP
        private async void RestorePipJavaScriptActions()
        {
            try
            {
                // Obtener WebView2 de la pestaña activa actual (ejecutar en hilo UI)
                Microsoft.Web.WebView2.Wpf.WebView2 activeWebView = null;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    activeWebView = GetActiveWebView();
                });

                if (activeWebView?.CoreWebView2 == null)
                {
                    return;
                }

                // Restaurar elementos eliminados de Tarkov Market (Tarkov Pilot)
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.TARKOV_MARGET_ELEMENT_RESTORE
                );
            }
            catch (Exception) { }
        }

        // Obtener WebView2 de la pestaña activa actual
        private Microsoft.Web.WebView2.Wpf.WebView2 GetActiveWebView()
        {
            try
            {
                var mainWindow = _mainWindow as MainWindow;
                if (
                    mainWindow?.TabContainer?.SelectedItem
                    is System.Windows.Controls.TabItem selectedTab
                )
                {
                    // Obtener WebView2 del diccionario _tabWebViews de MainWindow
                    var webViewField = typeof(MainWindow).GetField(
                        "_tabWebViews",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );

                    if (
                        webViewField?.GetValue(mainWindow)
                        is System.Collections.Generic.Dictionary<
                            System.Windows.Controls.TabItem,
                            Microsoft.Web.WebView2.Wpf.WebView2
                        > tabWebViews
                    )
                    {
                        if (tabWebViews.TryGetValue(selectedTab, out var webView))
                        {
                            return webView;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Guardar configuración de modo normal
        private void SaveNormalModeSettings()
        {
            try
            {
                var settings = Env.GetSettings();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    settings.normalLeft = _mainWindow.Left;
                    settings.normalTop = _mainWindow.Top;
                    settings.normalWidth = _mainWindow.Width;
                    settings.normalHeight = _mainWindow.Height;

                    Env.SetSettings(settings);
                    Settings.Save();
                });
            }
            catch (Exception) { }
        }

        // Restaurar configuración de UI de modo normal
        private async void RestoreNormalModeSettings()
        {
            try
            {
                // Eliminar superposición PiP de JavaScript (ejecutar fuera del hilo UI)
                var activeWebView = GetActiveWebView();
                if (activeWebView != null && activeWebView.CoreWebView2 != null)
                {
                    await activeWebView.CoreWebView2.ExecuteScriptAsync(
                        JavaScriptConstants.REMOVE_PIP_OVERLAY_SCRIPT
                    );
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Restaurar barra lateral de pestañas y restaurar TabControl a posición original
                    var tabSidebar =
                        _mainWindow.FindName("TabSidebar") as System.Windows.Controls.Border;
                    var tabContainer =
                        _mainWindow.FindName("TabContainer") as System.Windows.Controls.TabControl;

                    if (tabSidebar != null)
                    {
                        tabSidebar.Visibility = Visibility.Visible;
                    }

                    // Restaurar TabContainer a posición original y restaurar área de encabezado
                    if (tabContainer != null)
                    {
                        System.Windows.Controls.Grid.SetColumn(tabContainer, 1);
                        System.Windows.Controls.Grid.SetColumnSpan(tabContainer, 1);

                        // Restaurar área de encabezado (inicializar Margin)
                        tabContainer.Margin = new System.Windows.Thickness(0, 0, 0, 0);

                        // Restaurar Z-Index a original
                        System.Windows.Controls.Panel.SetZIndex(tabContainer, 100);
                    }
                });
            }
            catch (Exception) { }
        }

        // Obtener valor Transform por mapa
        private string GetMapTransform(AppSettings settings, string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
            {
                return MapTransformCalculator.CalculateFactoryMapTransform(300, 250);
            }

            // Verificar valor transform en configuración por mapa
            if (settings.mapSettings != null && settings.mapSettings.ContainsKey(mapName))
            {
                var mapSetting = settings.mapSettings[mapName];
                if (!string.IsNullOrEmpty(mapSetting.transform))
                {
                    return mapSetting.transform;
                }
            }

            // Si no hay transform guardado, usar valor predeterminado (actualmente fórmula de Factory)
            return MapTransformCalculator.CalculateFactoryMapTransform(300, 250);
        }

        // Obtener configuración por mapa
        private MapSetting GetMapSetting(AppSettings settings, string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
            {
                return new MapSetting();
            }

            // Verificar configuración por mapa
            if (settings.mapSettings != null && settings.mapSettings.ContainsKey(mapName))
            {
                return settings.mapSettings[mapName];
            }

            // Si no hay configuración guardada, crear nueva con valores predeterminados y guardar
            var defaultSetting = new MapSetting();
            if (settings.mapSettings == null)
            {
                settings.mapSettings = new System.Collections.Generic.Dictionary<
                    string,
                    MapSetting
                >();
            }
            settings.mapSettings[mapName] = defaultSetting;
            Env.SetSettings(settings);
            Settings.Save();

            return defaultSetting;
        }

        /// <summary>
        /// Eliminar manejador de eventos de cambio de tamaño de ventana
        /// </summary>
        private void UnregisterSizeChangedHandler()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _mainWindow.SizeChanged -= OnWindowSizeChanged;
                });
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Manejador de eventos de cambio de tamaño de ventana
        /// </summary>
        private async void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                // No procesar si no está en modo PiP
                if (!_isActive)
                    return;

                var settings = Env.GetSettings();

                // No procesar si la función de restauración automática está desactivada
                if (!settings.enableAutoRestore)
                    return;

                double currentWidth = e.NewSize.Width;
                double currentHeight = e.NewSize.Height;

                // No procesar si el tamaño es igual al anterior (prevenir procesamiento innecesario)
                if (
                    Math.Abs(currentWidth - _lastKnownWidth) < 1
                    && Math.Abs(currentHeight - _lastKnownHeight) < 1
                )
                    return;

                _lastKnownWidth = currentWidth;
                _lastKnownHeight = currentHeight;

                // Verificar si el tamaño actual es mayor o igual al umbral
                bool isLargeSize = IsLargeSize(currentWidth, currentHeight, settings);

                // Ejecutar JavaScript solo cuando hay cambio de estado
                if (isLargeSize && _elementsHidden)
                {
                    await RestoreElementsForLargeSize();
                    _elementsHidden = false;
                }
                else if (!isLargeSize && !_elementsHidden)
                {
                    await HideElementsForSmallSize();
                    _elementsHidden = true;
                }
                else { }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Verificar si el tamaño actual de la ventana es mayor o igual al umbral
        /// </summary>
        private bool IsLargeSize(double width, double height, AppSettings settings)
        {
            return width >= settings.restoreThresholdWidth
                && height >= settings.restoreThresholdHeight;
        }

        /// <summary>
        /// Restaurar elementos cuando el tamaño es grande
        /// </summary>
        private async Task RestoreElementsForLargeSize()
        {
            try
            {
                // Obtener WebView2 de la pestaña activa actual
                Microsoft.Web.WebView2.Wpf.WebView2 activeWebView = null;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    activeWebView = GetActiveWebView();
                });

                if (activeWebView?.CoreWebView2 == null)
                {
                    return;
                }

                // Restaurar elementos con JavaScript
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.RESTORE_ELEMENTS_FOR_LARGE_SIZE
                );
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Ocultar elementos cuando el tamaño es pequeño
        /// </summary>
        private async Task HideElementsForSmallSize()
        {
            try
            {
                // Obtener WebView2 de la pestaña activa actual
                Microsoft.Web.WebView2.Wpf.WebView2 activeWebView = null;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    activeWebView = GetActiveWebView();
                });

                if (activeWebView?.CoreWebView2 == null)
                {
                    return;
                }

                // Ocultar elementos con JavaScript
                await activeWebView.CoreWebView2.ExecuteScriptAsync(
                    JavaScriptConstants.HIDE_ELEMENTS_FOR_SMALL_SIZE
                );
            }
            catch (Exception) { }
        }
    }
}
