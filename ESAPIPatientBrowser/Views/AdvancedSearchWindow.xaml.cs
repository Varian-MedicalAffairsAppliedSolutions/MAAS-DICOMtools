using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ESAPIPatientBrowser.Models;
using ESAPIPatientBrowser.Services;
using MahApps.Metro.Controls;

namespace ESAPIPatientBrowser.Views
{
    public partial class AdvancedSearchWindow : MetroWindow
    {
        public AdvancedSearchCriteria SearchCriteria { get; private set; }
        public bool SearchInitiated { get; private set; }
        
        // Expose edited patient filter values to sync back to main UI
        public string PatientSearchText { get; private set; }
        public DateTime? FromDate { get; private set; }
        public DateTime? ToDate { get; private set; }
        public bool ShowOnlyPatientsWithPlans { get; private set; }
        
        // Delayed search properties
        private DispatcherTimer _delayTimer;
        private DispatcherTimer _countdownTimer;
        private DateTime _scheduledSearchTime;
        
        // Patient limit property
        public int? PatientLimit { get; private set; }

        public AdvancedSearchWindow(string searchText = "", DateTime? fromDate = null, DateTime? toDate = null, bool showOnlyPatientsWithPlans = true)
        {
            InitializeComponent();
            SearchInitiated = false;
            
            // Set values from main UI
            PatientSearchTextBox.Text = searchText;
            
            // Load session settings first (takes precedence over parameters)
            var sessionSettings = SettingsService.LoadSessionSettings();
            
            // Use session date range if available, otherwise use parameters or defaults
            if (sessionSettings.FromDate.HasValue || sessionSettings.ToDate.HasValue)
            {
                FromDatePicker.SelectedDate = sessionSettings.FromDate ?? DateTime.Now.AddYears(-5);
                ToDatePicker.SelectedDate = sessionSettings.ToDate ?? DateTime.Now;
            }
            else if (fromDate.HasValue || toDate.HasValue)
            {
                FromDatePicker.SelectedDate = fromDate ?? DateTime.Now.AddYears(-5);
                ToDatePicker.SelectedDate = toDate ?? DateTime.Now;
            }
            else
            {
                // Set default date range to 5 years back
                FromDatePicker.SelectedDate = DateTime.Now.AddYears(-5);
                ToDatePicker.SelectedDate = DateTime.Now;
            }
            
            // Load session checkbox states
            HasPlansCheckBox.IsChecked = sessionSettings.ShowOnlyPatientsWithPlans;
            DelaySearchCheckBox.IsChecked = sessionSettings.DelaySearchEnabled;
            DelayMinutesTextBox.Text = sessionSettings.DelayMinutes.ToString();
            LimitPatientsCheckBox.IsChecked = sessionSettings.LimitPatientsEnabled;
            PatientLimitTextBox.Text = sessionSettings.PatientLimit.ToString();
            
            // Load and populate saved criteria settings
            LoadSavedSettings();
        }

        /// <summary>
        /// Loads saved Advanced Search criteria and populates UI controls
        /// </summary>
        private void LoadSavedSettings()
        {
            try
            {
                var savedCriteria = SettingsService.LoadAdvancedSearchCriteria();
                System.Diagnostics.Debug.WriteLine("=== Loading Saved Settings ===");
                System.Diagnostics.Debug.WriteLine($"Saved criteria IsEmpty: {savedCriteria.IsEmpty()}");
                
                // Populate text fields
                TargetVolumeTextBox.Text = savedCriteria.TargetVolumeContains ?? string.Empty;
                PrescriptionSiteTextBox.Text = savedCriteria.PrescriptionSiteContains ?? string.Empty;
                PrescriptionTechniqueTextBox.Text = savedCriteria.PrescriptionTechniqueContains ?? string.Empty;
                PlanNameTextBox.Text = savedCriteria.PlanNameContains ?? string.Empty;
                PlanIntentTextBox.Text = savedCriteria.PlanIntentContains ?? string.Empty;
                ApprovalStatusTextBox.Text = savedCriteria.ApprovalStatusContains ?? string.Empty;
                StructureNameTextBox.Text = savedCriteria.StructureNameContains ?? string.Empty;
                MachineIdTextBox.Text = savedCriteria.MachineIdContains ?? string.Empty;
                EnergyTextBox.Text = savedCriteria.EnergyContains ?? string.Empty;
                MlcIdTextBox.Text = savedCriteria.MlcIdContains ?? string.Empty;
                MlcTypeTextBox.Text = savedCriteria.MlcTypeContains ?? string.Empty;
                BeamTechniqueTextBox.Text = savedCriteria.BeamTechniqueContains ?? string.Empty;
                PatientSexTextBox.Text = savedCriteria.PatientSexContains ?? string.Empty;
                HospitalTextBox.Text = savedCriteria.HospitalContains ?? string.Empty;
                CourseIntentTextBox.Text = savedCriteria.CourseIntentContains ?? string.Empty;
                CreationUserTextBox.Text = savedCriteria.CreationUserContains ?? string.Empty;
                PlanApproverTextBox.Text = savedCriteria.PlanApproverContains ?? string.Empty;
                TreatmentApproverTextBox.Text = savedCriteria.TreatmentApproverContains ?? string.Empty;
                ImageOrientationTextBox.Text = savedCriteria.ImageOrientationContains ?? string.Empty;
                
                // Populate numeric range fields
                MinDoseTextBox.Text = savedCriteria.MinDose?.ToString() ?? string.Empty;
                MaxDoseTextBox.Text = savedCriteria.MaxDose?.ToString() ?? string.Empty;
                MinDosePerFractionTextBox.Text = savedCriteria.MinDosePerFraction?.ToString() ?? string.Empty;
                MaxDosePerFractionTextBox.Text = savedCriteria.MaxDosePerFraction?.ToString() ?? string.Empty;
                MinFractionsTextBox.Text = savedCriteria.MinFractions?.ToString() ?? string.Empty;
                MaxFractionsTextBox.Text = savedCriteria.MaxFractions?.ToString() ?? string.Empty;
                MinControlPointsTextBox.Text = savedCriteria.MinControlPoints?.ToString() ?? string.Empty;
                MaxControlPointsTextBox.Text = savedCriteria.MaxControlPoints?.ToString() ?? string.Empty;
                
                // Populate date fields
                MinApprovalDatePicker.SelectedDate = savedCriteria.MinApprovalDate;
                MaxApprovalDatePicker.SelectedDate = savedCriteria.MaxApprovalDate;
                
                // Populate boolean ComboBoxes
                if (savedCriteria.IsDoseValid.HasValue)
                {
                    foreach (ComboBoxItem item in IsDoseValidComboBox.Items)
                    {
                        if (item.Tag is bool boolValue && boolValue == savedCriteria.IsDoseValid.Value)
                        {
                            IsDoseValidComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                
                if (savedCriteria.AutoCrop.HasValue)
                {
                    foreach (ComboBoxItem item in AutoCropComboBox.Items)
                    {
                        if (item.Tag is bool boolValue && boolValue == savedCriteria.AutoCrop.Value)
                        {
                            AutoCropComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                
                if (savedCriteria.HasDVHEstimates.HasValue)
                {
                    foreach (ComboBoxItem item in HasDVHEstimatesComboBox.Items)
                    {
                        if (item.Tag is bool boolValue && boolValue == savedCriteria.HasDVHEstimates.Value)
                        {
                            HasDVHEstimatesComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                
                // Populate HasTreatment checkbox
                HasTreatmentCheckBox.IsChecked = savedCriteria.HasTreatment ?? false;
            }
            catch (Exception ex)
            {
                // Don't show error to user, just log it
                System.Diagnostics.Debug.WriteLine($"Error loading Advanced Search settings: {ex.Message}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if delayed search is enabled
            if (DelaySearchCheckBox.IsChecked == true && _delayTimer == null)
            {
                // Start delayed search instead of immediate search
                StartDelayedSearch();
                return;
            }
            
            // Execute immediate search
            ExecuteSearch();
        }
        
        /// <summary>
        /// Executes the search with current criteria
        /// </summary>
        private void ExecuteSearch()
        {
            // Create criteria from UI inputs
            SearchCriteria = new AdvancedSearchCriteria
            {
                // Basic plan info
                TargetVolumeContains = TargetVolumeTextBox.Text?.Trim(),
                PrescriptionSiteContains = PrescriptionSiteTextBox.Text?.Trim(),
                PrescriptionTechniqueContains = PrescriptionTechniqueTextBox.Text?.Trim(),
                PlanNameContains = PlanNameTextBox.Text?.Trim(),
                PlanIntentContains = PlanIntentTextBox.Text?.Trim(),
                ApprovalStatusContains = ApprovalStatusTextBox.Text?.Trim(),
                
                // Structures
                StructureNameContains = StructureNameTextBox.Text?.Trim(),
                
                // Machine/Beam config
                MachineIdContains = MachineIdTextBox.Text?.Trim(),
                EnergyContains = EnergyTextBox.Text?.Trim(),
                MlcIdContains = MlcIdTextBox.Text?.Trim(),
                MlcTypeContains = MlcTypeTextBox.Text?.Trim(),
                BeamTechniqueContains = BeamTechniqueTextBox.Text?.Trim(),
                
                // Patient/Course
                PatientSexContains = PatientSexTextBox.Text?.Trim(),
                HospitalContains = HospitalTextBox.Text?.Trim(),
                CourseIntentContains = CourseIntentTextBox.Text?.Trim(),
                
                // Users
                CreationUserContains = CreationUserTextBox.Text?.Trim(),
                PlanApproverContains = PlanApproverTextBox.Text?.Trim(),
                TreatmentApproverContains = TreatmentApproverTextBox.Text?.Trim(),
                
                // Advanced
                ImageOrientationContains = ImageOrientationTextBox.Text?.Trim()
            };

            // Parse total dose range
            if (!string.IsNullOrWhiteSpace(MinDoseTextBox.Text))
            {
                if (double.TryParse(MinDoseTextBox.Text, out double minDose))
                {
                    SearchCriteria.MinDose = minDose;
                }
                else
                {
                    ShowValidationError("Minimum total dose must be a valid number.");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(MaxDoseTextBox.Text))
            {
                if (double.TryParse(MaxDoseTextBox.Text, out double maxDose))
                {
                    SearchCriteria.MaxDose = maxDose;
                }
                else
                {
                    ShowValidationError("Maximum total dose must be a valid number.");
                    return;
                }
            }

            if (SearchCriteria.MinDose.HasValue && SearchCriteria.MaxDose.HasValue &&
                SearchCriteria.MinDose.Value > SearchCriteria.MaxDose.Value)
            {
                ShowValidationError("Minimum total dose cannot be greater than maximum dose.");
                return;
            }

            // Parse dose per fraction range
            if (!string.IsNullOrWhiteSpace(MinDosePerFractionTextBox.Text))
            {
                if (double.TryParse(MinDosePerFractionTextBox.Text, out double minDpf))
                {
                    SearchCriteria.MinDosePerFraction = minDpf;
                }
                else
                {
                    ShowValidationError("Minimum dose per fraction must be a valid number.");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(MaxDosePerFractionTextBox.Text))
            {
                if (double.TryParse(MaxDosePerFractionTextBox.Text, out double maxDpf))
                {
                    SearchCriteria.MaxDosePerFraction = maxDpf;
                }
                else
                {
                    ShowValidationError("Maximum dose per fraction must be a valid number.");
                    return;
                }
            }

            if (SearchCriteria.MinDosePerFraction.HasValue && SearchCriteria.MaxDosePerFraction.HasValue &&
                SearchCriteria.MinDosePerFraction.Value > SearchCriteria.MaxDosePerFraction.Value)
            {
                ShowValidationError("Minimum dose per fraction cannot be greater than maximum.");
                return;
            }

            // Parse fractions range
            if (!string.IsNullOrWhiteSpace(MinFractionsTextBox.Text))
            {
                if (int.TryParse(MinFractionsTextBox.Text, out int minFx))
                {
                    SearchCriteria.MinFractions = minFx;
                }
                else
                {
                    ShowValidationError("Minimum fractions must be a valid integer.");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(MaxFractionsTextBox.Text))
            {
                if (int.TryParse(MaxFractionsTextBox.Text, out int maxFx))
                {
                    SearchCriteria.MaxFractions = maxFx;
                }
                else
                {
                    ShowValidationError("Maximum fractions must be a valid integer.");
                    return;
                }
            }

            if (SearchCriteria.MinFractions.HasValue && SearchCriteria.MaxFractions.HasValue &&
                SearchCriteria.MinFractions.Value > SearchCriteria.MaxFractions.Value)
            {
                ShowValidationError("Minimum fractions cannot be greater than maximum.");
                return;
            }

            // Parse control points range
            if (!string.IsNullOrWhiteSpace(MinControlPointsTextBox.Text))
            {
                if (int.TryParse(MinControlPointsTextBox.Text, out int minCp))
                {
                    SearchCriteria.MinControlPoints = minCp;
                }
                else
                {
                    ShowValidationError("Minimum control points must be a valid integer.");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(MaxControlPointsTextBox.Text))
            {
                if (int.TryParse(MaxControlPointsTextBox.Text, out int maxCp))
                {
                    SearchCriteria.MaxControlPoints = maxCp;
                }
                else
                {
                    ShowValidationError("Maximum control points must be a valid integer.");
                    return;
                }
            }

            if (SearchCriteria.MinControlPoints.HasValue && SearchCriteria.MaxControlPoints.HasValue &&
                SearchCriteria.MinControlPoints.Value > SearchCriteria.MaxControlPoints.Value)
            {
                ShowValidationError("Minimum control points cannot be greater than maximum.");
                return;
            }

            // Parse approval date range
            SearchCriteria.MinApprovalDate = MinApprovalDatePicker.SelectedDate;
            SearchCriteria.MaxApprovalDate = MaxApprovalDatePicker.SelectedDate;

            if (SearchCriteria.MinApprovalDate.HasValue && SearchCriteria.MaxApprovalDate.HasValue &&
                SearchCriteria.MinApprovalDate.Value > SearchCriteria.MaxApprovalDate.Value)
            {
                ShowValidationError("Minimum approval date cannot be after maximum approval date.");
                return;
            }

            // Parse boolean dropdowns
            if (IsDoseValidComboBox.SelectedItem is ComboBoxItem doseValidItem && doseValidItem.Tag != null)
            {
                SearchCriteria.IsDoseValid = (bool)doseValidItem.Tag;
            }

            if (AutoCropComboBox.SelectedItem is ComboBoxItem autoCropItem && autoCropItem.Tag != null)
            {
                SearchCriteria.AutoCrop = (bool)autoCropItem.Tag;
            }

            if (HasDVHEstimatesComboBox.SelectedItem is ComboBoxItem hasDVHItem && hasDVHItem.Tag != null)
            {
                SearchCriteria.HasDVHEstimates = (bool)hasDVHItem.Tag;
            }
            
            // Parse HasTreatment checkbox
            if (HasTreatmentCheckBox.IsChecked == true)
            {
                SearchCriteria.HasTreatment = true;
            }

            // Debug output to verify criteria
            System.Diagnostics.Debug.WriteLine("=== Search Criteria Created ===");
            System.Diagnostics.Debug.WriteLine($"IsEmpty: {SearchCriteria.IsEmpty()}");
            System.Diagnostics.Debug.WriteLine($"TargetVolumeContains: '{SearchCriteria.TargetVolumeContains}'");
            System.Diagnostics.Debug.WriteLine($"PrescriptionSiteContains: '{SearchCriteria.PrescriptionSiteContains}'");
            System.Diagnostics.Debug.WriteLine($"PlanNameContains: '{SearchCriteria.PlanNameContains}'");
            System.Diagnostics.Debug.WriteLine($"MinDose: {SearchCriteria.MinDose}");
            System.Diagnostics.Debug.WriteLine($"MaxDose: {SearchCriteria.MaxDose}");

            // Save criteria settings for next time (persisted to disk)
            SettingsService.SaveAdvancedSearchCriteria(SearchCriteria);

            // Capture patient filter values to sync back to main UI
            PatientSearchText = PatientSearchTextBox.Text?.Trim() ?? "";
            FromDate = FromDatePicker.SelectedDate;
            ToDate = ToDatePicker.SelectedDate;
            ShowOnlyPatientsWithPlans = HasPlansCheckBox.IsChecked ?? false;
            
            // Capture patient limit if enabled
            if (LimitPatientsCheckBox.IsChecked == true)
            {
                if (int.TryParse(PatientLimitTextBox.Text, out int limit) && limit > 0)
                {
                    PatientLimit = limit;
                }
                else
                {
                    ShowValidationError("Please enter a valid positive number for the patient limit.");
                    return;
                }
            }
            else
            {
                PatientLimit = null;
            }
            
            // Save session settings (in-memory only, for within-session persistence)
            SaveCurrentSessionSettings();

            SearchInitiated = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Save session settings even when canceling (for next time window opens)
            SaveCurrentSessionSettings();
            
            SearchInitiated = false;
            DialogResult = false;
            Close();
        }
        
        /// <summary>
        /// Saves current UI state to session settings (in-memory)
        /// </summary>
        private void SaveCurrentSessionSettings()
        {
            var sessionSettings = new AdvancedSearchSessionSettings
            {
                FromDate = FromDatePicker.SelectedDate,
                ToDate = ToDatePicker.SelectedDate,
                ShowOnlyPatientsWithPlans = HasPlansCheckBox.IsChecked ?? true,
                DelaySearchEnabled = DelaySearchCheckBox.IsChecked ?? false,
                DelayMinutes = int.TryParse(DelayMinutesTextBox.Text, out int mins) && mins > 0 ? mins : 30,
                LimitPatientsEnabled = LimitPatientsCheckBox.IsChecked ?? false,
                PatientLimit = int.TryParse(PatientLimitTextBox.Text, out int limit) && limit > 0 ? limit : 10
            };
            
            SettingsService.SaveSessionSettings(sessionSettings);
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            // Confirm with user
            var result = ThemedMessageBox.Show(
                "This will clear all filters and delete saved settings.\n\nAre you sure?",
                "Clear All Filters",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Clear all text fields
            TargetVolumeTextBox.Clear();
            PrescriptionSiteTextBox.Clear();
            PrescriptionTechniqueTextBox.Clear();
            PlanNameTextBox.Clear();
            PlanIntentTextBox.Clear();
            ApprovalStatusTextBox.Clear();
            StructureNameTextBox.Clear();
            MachineIdTextBox.Clear();
            EnergyTextBox.Clear();
            MlcIdTextBox.Clear();
            MlcTypeTextBox.Clear();
            BeamTechniqueTextBox.Clear();
            PatientSexTextBox.Clear();
            HospitalTextBox.Clear();
            CourseIntentTextBox.Clear();
            CreationUserTextBox.Clear();
            PlanApproverTextBox.Clear();
            TreatmentApproverTextBox.Clear();
            ImageOrientationTextBox.Clear();

            // Clear numeric fields
            MinDoseTextBox.Clear();
            MaxDoseTextBox.Clear();
            MinDosePerFractionTextBox.Clear();
            MaxDosePerFractionTextBox.Clear();
            MinFractionsTextBox.Clear();
            MaxFractionsTextBox.Clear();
            MinControlPointsTextBox.Clear();
            MaxControlPointsTextBox.Clear();

            // Clear date pickers
            MinApprovalDatePicker.SelectedDate = null;
            MaxApprovalDatePicker.SelectedDate = null;

            // Reset ComboBoxes to first item (Any)
            IsDoseValidComboBox.SelectedIndex = 0;
            AutoCropComboBox.SelectedIndex = 0;
            HasDVHEstimatesComboBox.SelectedIndex = 0;
            
            // Reset checkboxes
            HasTreatmentCheckBox.IsChecked = false;

            // Clear saved settings
            SettingsService.ClearAdvancedSearchCriteria();

            ThemedMessageBox.Show("All filters cleared and saved settings deleted.",
                "Filters Cleared",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowValidationError(string message)
        {
            ThemedMessageBox.Show(message, "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
        /// <summary>
        /// Handles the DelaySearchCheckBox checked event
        /// </summary>
        private void DelaySearchCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DelaySearchPanel.Visibility = Visibility.Visible;
            UpdateScheduledTime();
        }
        
        /// <summary>
        /// Handles the DelaySearchCheckBox unchecked event
        /// </summary>
        private void DelaySearchCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DelaySearchPanel.Visibility = Visibility.Collapsed;
            
            // Cancel any existing timer
            if (_delayTimer != null)
            {
                _delayTimer.Stop();
                _delayTimer = null;
            }
            
            // Stop countdown timer
            StopCountdownTimer();
            
            ScheduledTimeDisplay.Text = "";
        }
        
        /// <summary>
        /// Number validation for TextBox input
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Only allow digits
            e.Handled = !int.TryParse(e.Text, out _);
        }
        
        /// <summary>
        /// Handles TextChanged event for delay minutes textbox
        /// </summary>
        private void DelayMinutesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DelaySearchCheckBox.IsChecked == true)
            {
                UpdateScheduledTime();
            }
        }
        
        /// <summary>
        /// Handles the LimitPatientsCheckBox checked event
        /// </summary>
        private void LimitPatientsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LimitPatientsPanel.Visibility = Visibility.Visible;
        }
        
        /// <summary>
        /// Handles the LimitPatientsCheckBox unchecked event
        /// </summary>
        private void LimitPatientsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LimitPatientsPanel.Visibility = Visibility.Collapsed;
        }
        
        /// <summary>
        /// Updates the scheduled search time display
        /// </summary>
        private void UpdateScheduledTime()
        {
            if (!int.TryParse(DelayMinutesTextBox.Text, out int minutes) || minutes <= 0)
            {
                ScheduledTimeDisplay.Text = "Please enter a valid number of minutes";
                ScheduledTimeDisplay.Foreground = System.Windows.Media.Brushes.Orange;
                return;
            }
            
            _scheduledSearchTime = DateTime.Now.AddMinutes(minutes);
            ScheduledTimeDisplay.Text = $"Search will start at: {_scheduledSearchTime:h:mm:ss tt}";
            ScheduledTimeDisplay.Foreground = System.Windows.Media.Brushes.LightGreen;
        }
        
        /// <summary>
        /// Initiates a delayed search by starting a timer
        /// </summary>
        private void StartDelayedSearch()
        {
            if (!int.TryParse(DelayMinutesTextBox.Text, out int minutes) || minutes <= 0)
            {
                ShowValidationError("Please enter a valid number of minutes for the delayed search.");
                return;
            }
            
            // Calculate scheduled time
            _scheduledSearchTime = DateTime.Now.AddMinutes(minutes);
            
            // Create and start timer
            _delayTimer = new DispatcherTimer();
            _delayTimer.Interval = TimeSpan.FromMinutes(minutes);
            _delayTimer.Tick += DelayTimer_Tick;
            _delayTimer.Start();
            
            // Start countdown timer
            StartCountdownTimer();
            
            // Minimize the window (assume user is gone)
            this.WindowState = WindowState.Minimized;
            
            // Update display
            ScheduledTimeDisplay.Text = $"Search scheduled for: {_scheduledSearchTime:h:mm:ss tt} (Window minimized)";
            ScheduledTimeDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
        }
        
        /// <summary>
        /// Starts the countdown timer display
        /// </summary>
        private void StartCountdownTimer()
        {
            // Show the countdown timer
            CountdownTimerBorder.Visibility = Visibility.Visible;
            
            // Set target time
            CountdownTargetTime.Text = $"Target: {_scheduledSearchTime:h:mm:ss tt}";
            
            // Create countdown timer that updates every second
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
            
            // Update immediately
            UpdateCountdownDisplay();
        }
        
        /// <summary>
        /// Updates the countdown display
        /// </summary>
        private void UpdateCountdownDisplay()
        {
            TimeSpan remaining = _scheduledSearchTime - DateTime.Now;
            
            if (remaining.TotalSeconds <= 0)
            {
                CountdownTimerText.Text = "00:00:00";
                CountdownTimerText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            
            // Format as HH:MM:SS
            CountdownTimerText.Text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            
            // Change color based on time remaining
            if (remaining.TotalMinutes <= 1)
            {
                CountdownTimerText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (remaining.TotalMinutes <= 5)
            {
                CountdownTimerText.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                CountdownTimerText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2E, 0xCC, 0x71)); // Green
            }
        }
        
        /// <summary>
        /// Countdown timer tick event
        /// </summary>
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            UpdateCountdownDisplay();
        }
        
        /// <summary>
        /// Stops and hides the countdown timer
        /// </summary>
        private void StopCountdownTimer()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer = null;
            }
            CountdownTimerBorder.Visibility = Visibility.Collapsed;
        }
        
        /// <summary>
        /// Timer tick event that triggers the search
        /// </summary>
        private void DelayTimer_Tick(object sender, EventArgs e)
        {
            // Stop and dispose timers
            _delayTimer.Stop();
            _delayTimer = null;
            StopCountdownTimer();
            
            // Restore window if minimized
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
                this.Activate();
            }
            
            // Uncheck the delay checkbox to prevent infinite loop
            DelaySearchCheckBox.IsChecked = false;
            
            // Execute the search directly (don't call SearchButton_Click which would start another delay)
            ExecuteSearch();
        }
    }
}
