using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ESAPIPatientBrowser.Models
{
    public class PlanInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _patientId;
        private string _courseId;
        private string _planId;
        private DateTime? _creationDate;
        private string _approvalStatus;
        private double _totalDose;
        private int _numberOfFractions;
        private string _structureSetId;
        private string _targetVolumeId;
        private string _prescriptionSite;
        private string _prescriptionPhase;
        private string _prescriptionTechnique;
        private string _planIntent;
        private List<string> _structureIds;
        private string _machineId;
        private string _energy;
        private string _photonCalculationModel;
        private string _patientSex;
        private string _hospital;
        private string _courseIntent;
        private double _dosePerFraction;
        private string _mlcId;
        private string _mlcType;
        private string _beamTechnique;
        private int _numberOfControlPoints;
        private bool? _autoCrop;
        private string _creationUser;
        private string _planApprover;
        private string _treatmentApprover;
        private DateTime? _approvalDate;
        private string _imageOrientation;
        private bool? _isDoseValid;
        private bool? _hasDVHEstimates;

        public string PatientId
        {
            get => _patientId;
            set { _patientId = value; OnPropertyChanged(); }
        }

        public string CourseId
        {
            get => _courseId;
            set { _courseId = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
        }

        public string PlanId
        {
            get => _planId;
            set { _planId = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
        }

        public DateTime? CreationDate
        {
            get => _creationDate;
            set { _creationDate = value; OnPropertyChanged(); }
        }

        public string ApprovalStatus
        {
            get => _approvalStatus;
            set { _approvalStatus = value; OnPropertyChanged(); }
        }

        public double TotalDose
        {
            get => _totalDose;
            set { _totalDose = value; OnPropertyChanged(); }
        }

        public int NumberOfFractions
        {
            get => _numberOfFractions;
            set { _numberOfFractions = value; OnPropertyChanged(); }
        }

        public string StructureSetId
        {
            get => _structureSetId;
            set { _structureSetId = value; OnPropertyChanged(); }
        }

        public string TargetVolumeId
        {
            get => _targetVolumeId;
            set { _targetVolumeId = value; OnPropertyChanged(); }
        }

        public string PrescriptionSite
        {
            get => _prescriptionSite;
            set { _prescriptionSite = value; OnPropertyChanged(); }
        }

        public string PrescriptionPhase
        {
            get => _prescriptionPhase;
            set { _prescriptionPhase = value; OnPropertyChanged(); }
        }

        public string PrescriptionTechnique
        {
            get => _prescriptionTechnique;
            set { _prescriptionTechnique = value; OnPropertyChanged(); }
        }

        public string PlanIntent
        {
            get => _planIntent;
            set { _planIntent = value; OnPropertyChanged(); }
        }

        public List<string> StructureIds
        {
            get => _structureIds ?? (_structureIds = new List<string>());
            set { _structureIds = value; OnPropertyChanged(); }
        }

        public string MachineId
        {
            get => _machineId;
            set { _machineId = value; OnPropertyChanged(); }
        }

        public string Energy
        {
            get => _energy;
            set { _energy = value; OnPropertyChanged(); }
        }

        public string PhotonCalculationModel
        {
            get => _photonCalculationModel;
            set { _photonCalculationModel = value; OnPropertyChanged(); }
        }

        public string PatientSex
        {
            get => _patientSex;
            set { _patientSex = value; OnPropertyChanged(); }
        }

        public string Hospital
        {
            get => _hospital;
            set { _hospital = value; OnPropertyChanged(); }
        }

        public string CourseIntent
        {
            get => _courseIntent;
            set { _courseIntent = value; OnPropertyChanged(); }
        }

        public double DosePerFraction
        {
            get => _dosePerFraction;
            set { _dosePerFraction = value; OnPropertyChanged(); }
        }

        public string MlcId
        {
            get => _mlcId;
            set { _mlcId = value; OnPropertyChanged(); }
        }

        public string MlcType
        {
            get => _mlcType;
            set { _mlcType = value; OnPropertyChanged(); }
        }

        public string BeamTechnique
        {
            get => _beamTechnique;
            set { _beamTechnique = value; OnPropertyChanged(); }
        }

        public int NumberOfControlPoints
        {
            get => _numberOfControlPoints;
            set { _numberOfControlPoints = value; OnPropertyChanged(); }
        }

        public bool? AutoCrop
        {
            get => _autoCrop;
            set { _autoCrop = value; OnPropertyChanged(); }
        }

        public string CreationUser
        {
            get => _creationUser;
            set { _creationUser = value; OnPropertyChanged(); }
        }

        public string PlanApprover
        {
            get => _planApprover;
            set { _planApprover = value; OnPropertyChanged(); }
        }

        public string TreatmentApprover
        {
            get => _treatmentApprover;
            set { _treatmentApprover = value; OnPropertyChanged(); }
        }

        public DateTime? ApprovalDate
        {
            get => _approvalDate;
            set { _approvalDate = value; OnPropertyChanged(); }
        }

        public string ImageOrientation
        {
            get => _imageOrientation;
            set { _imageOrientation = value; OnPropertyChanged(); }
        }

        public bool? IsDoseValid
        {
            get => _isDoseValid;
            set { _isDoseValid = value; OnPropertyChanged(); }
        }

        public bool? HasDVHEstimates
        {
            get => _hasDVHEstimates;
            set { _hasDVHEstimates = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string DisplayText => $"{CourseId}: {PlanId}";

        public string DetailText => $"{DisplayText} - {ApprovalStatus} ({TotalDose:F1} Gy/{NumberOfFractions} fx)";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
