using System;
using System.Windows;
using System.Windows.Controls;
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

        public AdvancedSearchWindow(string searchText = "", DateTime? fromDate = null, DateTime? toDate = null, bool showOnlyPatientsWithPlans = true)
        {
            InitializeComponent();
            SearchInitiated = false;
            
            // Set values from main UI
            PatientSearchTextBox.Text = searchText;
            
            // Set default date range to 5 years back if no dates provided
            // This helps prevent searching through too many patients
            if (!fromDate.HasValue && !toDate.HasValue)
            {
                FromDatePicker.SelectedDate = DateTime.Now.AddYears(-5);
                ToDatePicker.SelectedDate = DateTime.Now;
            }
            else
            {
                FromDatePicker.SelectedDate = fromDate;
                ToDatePicker.SelectedDate = toDate;
            }
            
            HasPlansCheckBox.IsChecked = showOnlyPatientsWithPlans;
            
            // Load and populate saved settings
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
            }
            catch (Exception ex)
            {
                // Don't show error to user, just log it
                System.Diagnostics.Debug.WriteLine($"Error loading Advanced Search settings: {ex.Message}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
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

            // Debug output to verify criteria
            System.Diagnostics.Debug.WriteLine("=== Search Criteria Created ===");
            System.Diagnostics.Debug.WriteLine($"IsEmpty: {SearchCriteria.IsEmpty()}");
            System.Diagnostics.Debug.WriteLine($"TargetVolumeContains: '{SearchCriteria.TargetVolumeContains}'");
            System.Diagnostics.Debug.WriteLine($"PrescriptionSiteContains: '{SearchCriteria.PrescriptionSiteContains}'");
            System.Diagnostics.Debug.WriteLine($"PlanNameContains: '{SearchCriteria.PlanNameContains}'");
            System.Diagnostics.Debug.WriteLine($"MinDose: {SearchCriteria.MinDose}");
            System.Diagnostics.Debug.WriteLine($"MaxDose: {SearchCriteria.MaxDose}");

            // Save settings for next time
            SettingsService.SaveAdvancedSearchCriteria(SearchCriteria);

            // Capture patient filter values to sync back to main UI
            PatientSearchText = PatientSearchTextBox.Text?.Trim() ?? "";
            FromDate = FromDatePicker.SelectedDate;
            ToDate = ToDatePicker.SelectedDate;
            ShowOnlyPatientsWithPlans = HasPlansCheckBox.IsChecked ?? false;

            SearchInitiated = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SearchInitiated = false;
            DialogResult = false;
            Close();
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
    }
}
