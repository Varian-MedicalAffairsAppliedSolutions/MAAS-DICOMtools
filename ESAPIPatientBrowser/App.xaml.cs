using System;
using System.Linq;
using System.Windows;
using ESAPIPatientBrowser.Views;
using ESAPIApp = VMS.TPS.Common.Model.API.Application;

namespace ESAPIPatientBrowser
{
    public partial class App : System.Windows.Application
    {
        private ESAPIApp _esapiApplication;
        private bool _isInitializingEsapi = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Check if a patient ID was passed as argument
                string initialPatientId = null;
                if (e.Args.Length > 0 && !string.IsNullOrWhiteSpace(e.Args[0]))
                {
                    initialPatientId = e.Args[0].Trim('"');
                }

                // Create main window WITHOUT ESAPI first - defer ESAPI creation
                var mainWindow = new Views.MainWindow(null, initialPatientId);
                mainWindow.Show();
                
                // Try multiple approaches to ensure ESAPI gets initialized
                
                // Approach 1: Use Loaded event
                mainWindow.Loaded += async (sender, args) =>
                {
                    System.Diagnostics.Debug.WriteLine("App.OnStartup: MainWindow.Loaded fired");
                    await InitializeESAPIAsync(mainWindow);
                };
                
                // Approach 2: Use ContentRendered as backup (fires after Loaded)
                mainWindow.ContentRendered += async (sender, args) =>
                {
                    System.Diagnostics.Debug.WriteLine("App.OnStartup: MainWindow.ContentRendered fired");
                    // Only initialize if not already done
                    if (_esapiApplication == null)
                    {
                        await InitializeESAPIAsync(mainWindow);
                    }
                };
                
                // Approach 3: Use a timer as final fallback
                var initTimer = new System.Windows.Threading.DispatcherTimer();
                initTimer.Interval = TimeSpan.FromSeconds(2);
                initTimer.Tick += async (sender, args) =>
                {
                    System.Diagnostics.Debug.WriteLine("App.OnStartup: DispatcherTimer tick fired");
                    initTimer.Stop();
                    if (_esapiApplication == null)
                    {
                        await InitializeESAPIAsync(mainWindow);
                    }
                };
                initTimer.Start();
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Failed to initialize Patient List Builder:\n\n{ex.Message}",
                                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private async System.Threading.Tasks.Task InitializeESAPIAsync(Views.MainWindow mainWindow)
        {
            if (_esapiApplication != null)
            {
                System.Diagnostics.Debug.WriteLine("InitializeESAPIAsync: already initialized; returning");
                return; // already initialized
            }
            if (_isInitializingEsapi)
            {
                System.Diagnostics.Debug.WriteLine("InitializeESAPIAsync: initialization already in progress; returning");
                return; // guard against concurrent initialization
            }

            _isInitializingEsapi = true;
            try
            {
                // Update title to show ESAPI creation in progress
                mainWindow.Title = "Patient List Builder - Creating ESAPI Connection...";
                
                // Small delay to ensure UI is fully rendered
                System.Diagnostics.Debug.WriteLine("InitializeESAPIAsync: delaying 500ms to allow UI to settle");
                await System.Threading.Tasks.Task.Delay(500);
                
                // Show debug message
                System.Diagnostics.Debug.WriteLine("InitializeESAPIAsync: Attempting to create ESAPI application");
                
                // Now create ESAPI application
                _esapiApplication = ESAPIApp.CreateApplication();
                
                System.Diagnostics.Debug.WriteLine("InitializeESAPIAsync: ESAPI application created");
                
                // Initialize the window with ESAPI
                mainWindow.InitializeWithESAPI(_esapiApplication);
                
                System.Diagnostics.Debug.WriteLine("InitializeESAPIAsync: MainWindow initialized with ESAPI");
            }
            catch (Exception esapiEx)
            {
                mainWindow.Title = "Patient List Builder - ESAPI Connection Failed";
                System.Diagnostics.Debug.WriteLine($"InitializeESAPIAsync: ESAPI initialization failed: {esapiEx.Message}\n{esapiEx.StackTrace}");
                ThemedMessageBox.Show($"Failed to initialize ESAPI:\n\n{esapiEx.Message}\n\nStack Trace:\n{esapiEx.StackTrace}",
                                "ESAPI Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
            finally
            {
                _isInitializingEsapi = false;
                System.Diagnostics.Debug.WriteLine("InitializeESAPIAsync: initialization guard reset");
            }
        }

        public async System.Threading.Tasks.Task TryManualInitialization(Views.MainWindow mainWindow)
        {
            if (_esapiApplication == null)
            {
                await InitializeESAPIAsync(mainWindow);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Clean up ESAPI resources
                _esapiApplication?.Dispose();
            }
            catch (Exception ex)
            {
                // Log but don't prevent shutdown
                System.Diagnostics.Debug.WriteLine($"Error disposing ESAPI application: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}
