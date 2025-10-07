using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace ESAPIPatientBrowser.Models
{
    public class PatientInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _patientId;
        private string _firstName;
        private string _lastName;
        private DateTime? _dateOfBirth;
        private DateTime? _creationDate;
        private List<PlanInfo> _plans;

        public string PatientId
        {
            get => _patientId;
            set { _patientId = value; OnPropertyChanged(); }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); }
        }

        public string FullName => $"{LastName}, {FirstName}";

        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set { _dateOfBirth = value; OnPropertyChanged(); }
        }

        public DateTime? CreationDate
        {
            get => _creationDate;
            set { _creationDate = value; OnPropertyChanged(); }
        }

        public List<PlanInfo> Plans
        {
            get => _plans ?? (_plans = new List<PlanInfo>());
            set
            {
                // Unsubscribe from previous plan events
                if (_plans != null)
                {
                    foreach (var plan in _plans)
                    {
                        plan.PropertyChanged -= OnPlanPropertyChanged;
                    }
                }

                _plans = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlanCount));
                OnPropertyChanged(nameof(HasPlansLoaded));
                OnPropertyChanged(nameof(SelectedPlanCount));
                OnPropertyChanged(nameof(PlanSelectionSummary));

                // Subscribe to new plan events
                if (_plans != null)
                {
                    foreach (var plan in _plans)
                    {
                        plan.PropertyChanged += OnPlanPropertyChanged;
                    }
                }
            }
        }

        public int PlanCount => Plans?.Count ?? 0;
        public int SelectedPlanCount => Plans?.Count(p => p.IsSelected) ?? 0;
        public string PlanSelectionSummary => $"{SelectedPlanCount}/{PlanCount}";
        public bool HasSelectedPlans => SelectedPlanCount > 0;
        public bool HasPlansLoaded => Plans != null && Plans.Count > 0;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string DisplayText => $"{PatientId} - {FullName} ({PlanCount} plans)";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnPlanPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlanInfo.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedPlanCount));
                OnPropertyChanged(nameof(PlanSelectionSummary));
                OnPropertyChanged(nameof(HasSelectedPlans));
            }
        }
    }
}
