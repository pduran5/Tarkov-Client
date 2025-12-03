using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace TarkovClient
{
    public partial class PipWindow : Window
    {
        private PipController _controller;
        private System.Windows.Threading.DispatcherTimer _fadeOutTimer;

        public WebView2 WebView => PipWebView;

        public PipWindow(PipController controller)
        {
            InitializeComponent();
            _controller = controller;

            // Configuración del temporizador de desvanecimiento
            _fadeOutTimer = new System.Windows.Threading.DispatcherTimer();
            _fadeOutTimer.Interval = TimeSpan.FromSeconds(2);
            _fadeOutTimer.Tick += (s, e) => FadeOutControls();

            // Eventos de ventana
            this.LocationChanged += OnLocationChanged;
            this.SizeChanged += OnSizeChanged;
        }

        // Inicializar WebView2
        public async Task InitializeWebView()
        {
            try
            {
                // Usar la misma UserDataFolder que la ventana principal
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TarkovClient",
                    "WebView2"
                );

                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                await PipWebView.EnsureCoreWebView2Async(environment);

                // Configuración de optimización dedicada para PiP
                PipWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                PipWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                PipWebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
                PipWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                PipWebView.CoreWebView2.Settings.AreHostObjectsAllowed = false;

                // Cargar la misma URL que la ventana principal
                PipWebView.Source = new Uri(Env.WebsiteUrl);
            }
            catch (Exception) { }
        }

        #region Interacción de hover

        // Entrada del mouse
        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _fadeOutTimer.Stop();
            FadeInControls();
        }

        // Salida del mouse
        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _fadeOutTimer.Start();
        }

        // Desvanecer entrada del panel de control
        private void FadeInControls()
        {
            var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(300));
            ControlOverlay.BeginAnimation(OpacityProperty, fadeIn);
        }

        // Desvanecer salida del panel de control
        private void FadeOutControls()
        {
            _fadeOutTimer.Stop();
            var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
            ControlOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }

        #endregion

        #region Manipulación de ventana

        // Clic en área de arrastre - Mover ventana
        private void DragArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // Clic en manejador de cambio de tamaño
        private void ResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Cambio de tamaño usando API de Windows
                ResizeWindow();
            }
        }

        // Cambio de tamaño de ventana a través de API de Windows
        private void ResizeWindow()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            // Iniciar cambio de tamaño
            var msg = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
            if (msg != null)
            {
                // Establecer cursor del mouse en esquina inferior derecha
                this.Cursor = System.Windows.Input.Cursors.SizeNWSE;

                // Procesar cambio de tamaño con eventos del mouse
                this.MouseMove += OnResizeMouseMove;
                this.MouseLeftButtonUp += OnResizeMouseUp;
                this.CaptureMouse();
            }
        }

        private void OnResizeMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(this);

                // Límite de tamaño mínimo/máximo
                var newWidth = Math.Max(MinWidth, Math.Min(MaxWidth, position.X + 10));
                var newHeight = Math.Max(MinHeight, Math.Min(MaxHeight, position.Y + 10));

                this.Width = newWidth;
                this.Height = newHeight;
            }
        }

        private void OnResizeMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Cursor = System.Windows.Input.Cursors.Arrow;
            this.MouseMove -= OnResizeMouseMove;
            this.MouseLeftButtonUp -= OnResizeMouseUp;
            this.ReleaseMouseCapture();

            // Cambio de tamaño completado
        }

        // Clic en botón cerrar
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _controller?.HidePip();
        }

        #endregion

        #region Evento de guardado de configuración

        // Al cambiar ubicación
        private void OnLocationChanged(object sender, EventArgs e)
        {
            // No es necesario guardar ubicación en PiP nativo del navegador
        }

        // Al cambiar tamaño
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // No es necesario guardar tamaño en PiP nativo del navegador
        }

        #endregion

        // Limpieza al cerrar ventana
        protected override void OnClosed(EventArgs e)
        {
            _fadeOutTimer?.Stop();
            PipWebView?.Dispose();
            base.OnClosed(e);
        }
    }
}
