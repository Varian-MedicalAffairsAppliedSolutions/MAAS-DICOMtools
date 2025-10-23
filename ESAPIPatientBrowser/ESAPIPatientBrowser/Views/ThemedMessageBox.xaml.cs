using System;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;

namespace ESAPIPatientBrowser.Views
{
    /// <summary>
    /// Themed message box matching the application's dark teal/orange theme
    /// </summary>
    public partial class ThemedMessageBox : MetroWindow
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        private ThemedMessageBox(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();
            
            Title = title;
            MessageTextBlock.Text = message;
            
            // Set icon based on MessageBoxImage
            SetIcon(icon);
            
            // Add buttons based on MessageBoxButton
            AddButtons(button);
        }

        /// <summary>
        /// Shows a themed message box
        /// </summary>
        public static MessageBoxResult Show(string message, string title = "Message", 
            MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            var msgBox = new ThemedMessageBox(message, title, button, icon);
            msgBox.Owner = Application.Current.MainWindow;
            msgBox.ShowDialog();
            return msgBox.Result;
        }

        private void SetIcon(MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Error: // Also covers Hand and Stop (same value)
                    IconControl.Kind = PackIconFontAwesomeKind.ExclamationCircleSolid;
                    IconControl.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(220, 53, 69)); // Red
                    break;

                case MessageBoxImage.Warning: // Also covers Exclamation (same value)
                    IconControl.Kind = PackIconFontAwesomeKind.ExclamationTriangleSolid;
                    IconControl.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(236, 102, 2)); // Orange
                    break;

                case MessageBoxImage.Question:
                    IconControl.Kind = PackIconFontAwesomeKind.QuestionCircleSolid;
                    IconControl.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0, 153, 153)); // Teal
                    break;

                case MessageBoxImage.Information: // Also covers Asterisk (same value)
                    IconControl.Kind = PackIconFontAwesomeKind.InfoCircleSolid;
                    IconControl.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0, 153, 153)); // Teal
                    break;

                case MessageBoxImage.None:
                default:
                    IconControl.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void AddButtons(MessageBoxButton button)
        {
            ButtonPanel.Children.Clear();

            switch (button)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", MessageBoxResult.OK, isDefault: true);
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton("Cancel", MessageBoxResult.Cancel, isCancel: true);
                    AddButton("OK", MessageBoxResult.OK, isDefault: true);
                    break;

                case MessageBoxButton.YesNo:
                    AddButton("No", MessageBoxResult.No);
                    AddButton("Yes", MessageBoxResult.Yes, isDefault: true);
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton("Cancel", MessageBoxResult.Cancel, isCancel: true);
                    AddButton("No", MessageBoxResult.No);
                    AddButton("Yes", MessageBoxResult.Yes, isDefault: true);
                    break;
            }
        }

        private void AddButton(string content, MessageBoxResult result, bool isDefault = false, bool isCancel = false)
        {
            var button = new System.Windows.Controls.Button
            {
                Content = content,
                Style = (Style)FindResource("DialogButtonStyle"),
                IsDefault = isDefault,
                IsCancel = isCancel
            };

            button.Click += (s, e) =>
            {
                Result = result;
                DialogResult = true;
                Close();
            };

            ButtonPanel.Children.Add(button);
        }
    }
}

