using System;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Data;
using System.Globalization;
using ESAPIPatientBrowser.ViewModels;
using ESAPIApp = VMS.TPS.Common.Model.API.Application;
using System.IO;
using MahApps.Metro.Controls;

namespace ESAPIPatientBrowser.Views
{
    // Converter to show progress bar only when progress > 0
    public class ProgressToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                return progress > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow : MetroWindow
    {
        private string _initialPatientId;
        
        public MainWindow(ESAPIApp esapiApplication, string initialPatientId = null)
        {
            InitializeComponent();
            _initialPatientId = initialPatientId;
            
            if (esapiApplication != null)
            {
                // Traditional initialization with ESAPI
                InitializeWithESAPI(esapiApplication);
            }
            else
            {
                // Deferred initialization - show loading message
                Title = "Patient List Builder - Initializing...";
                
                // Add a manual initialization button as backup
                var initButton = new System.Windows.Controls.Button
                {
                    Content = "Click here if initialization is stuck",
                    Margin = new Thickness(10),
                    Padding = new Thickness(10, 5, 10, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                initButton.Click += (s, e) =>
                {
                    // Try to get the App instance and call initialization
                    if (System.Windows.Application.Current is App app)
                    {
                        _ = app.TryManualInitialization(this);
                    }
                };
                
                // Add the button to the window temporarily
                if (Content == null)
                {
                    Content = initButton;
                }
            }
        }
        

        // Open Varian LUSLA link in default browser
        private void VarianLicense_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                // no-op
            }
            e.Handled = true;
        }
        public void InitializeWithESAPI(ESAPIApp esapiApplication)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("InitializeWithESAPI: start");
                // Update title to show successful initialization
                Title = "Patient List Builder";
                
                // Do not clear Content here; keep the XAML UI intact
                // Content = null;
                
                System.Diagnostics.Debug.WriteLine("InitializeWithESAPI: setting DataContext");
                // Create the ViewModel with ESAPI
                DataContext = new MainViewModel(esapiApplication, _initialPatientId);
                
                // Show success message in status (if ViewModel has a status property)
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.StatusMessage = "Initialized successfully - Ready to search patients";
                }
                
                System.Diagnostics.Debug.WriteLine("InitializeWithESAPI: completed successfully");
            }
            catch (Exception ex)
            {
                // Update title to show error state
                Title = "Patient List Builder - Initialization Failed";
                System.Diagnostics.Debug.WriteLine($"Error in InitializeWithESAPI: {ex.Message}\n{ex.StackTrace}");
                ThemedMessageBox.Show($"Error during initialization:\n{ex.Message}", 
                                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
    }
}
