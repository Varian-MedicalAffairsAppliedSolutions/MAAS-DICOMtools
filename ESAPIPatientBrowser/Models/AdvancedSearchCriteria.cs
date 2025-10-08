using System;

namespace ESAPIPatientBrowser.Models
{
    public class AdvancedSearchCriteria
    {
        // Existing filters
        public string TargetVolumeContains { get; set; }
        public string PrescriptionSiteContains { get; set; }
        public string PrescriptionTechniqueContains { get; set; }
        public string PlanNameContains { get; set; }
        public string StructureNameContains { get; set; }
        public string MachineIdContains { get; set; }
        public string EnergyContains { get; set; }
        public double? MinDose { get; set; }
        public double? MaxDose { get; set; }
        public string PlanIntentContains { get; set; }
        public string ApprovalStatusContains { get; set; }
        
        // High priority filters
        public int? MinFractions { get; set; }
        public int? MaxFractions { get; set; }
        public double? MinDosePerFraction { get; set; }
        public double? MaxDosePerFraction { get; set; }
        public string MlcTypeContains { get; set; }
        public string BeamTechniqueContains { get; set; }
        public int? MinControlPoints { get; set; }
        public int? MaxControlPoints { get; set; }
        public string PatientSexContains { get; set; }
        
        // Medium priority filters
        public string HospitalContains { get; set; }
        public string CourseIntentContains { get; set; }
        public string MlcIdContains { get; set; }
        public DateTime? MinApprovalDate { get; set; }
        public DateTime? MaxApprovalDate { get; set; }
        public string CreationUserContains { get; set; }
        public string PlanApproverContains { get; set; }
        public string TreatmentApproverContains { get; set; }
        
        // Lower priority filters
        public string ImageOrientationContains { get; set; }
        public bool? IsDoseValid { get; set; }
        public bool? AutoCrop { get; set; }
        public bool? HasDVHEstimates { get; set; }
        
        // Treatment filter
        public bool? HasTreatment { get; set; }

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(TargetVolumeContains) &&
                   string.IsNullOrWhiteSpace(PrescriptionSiteContains) &&
                   string.IsNullOrWhiteSpace(PrescriptionTechniqueContains) &&
                   string.IsNullOrWhiteSpace(PlanNameContains) &&
                   string.IsNullOrWhiteSpace(StructureNameContains) &&
                   string.IsNullOrWhiteSpace(MachineIdContains) &&
                   string.IsNullOrWhiteSpace(EnergyContains) &&
                   !MinDose.HasValue &&
                   !MaxDose.HasValue &&
                   string.IsNullOrWhiteSpace(PlanIntentContains) &&
                   string.IsNullOrWhiteSpace(ApprovalStatusContains) &&
                   !MinFractions.HasValue &&
                   !MaxFractions.HasValue &&
                   !MinDosePerFraction.HasValue &&
                   !MaxDosePerFraction.HasValue &&
                   string.IsNullOrWhiteSpace(MlcTypeContains) &&
                   string.IsNullOrWhiteSpace(BeamTechniqueContains) &&
                   !MinControlPoints.HasValue &&
                   !MaxControlPoints.HasValue &&
                   string.IsNullOrWhiteSpace(PatientSexContains) &&
                   string.IsNullOrWhiteSpace(HospitalContains) &&
                   string.IsNullOrWhiteSpace(CourseIntentContains) &&
                   string.IsNullOrWhiteSpace(MlcIdContains) &&
                   !MinApprovalDate.HasValue &&
                   !MaxApprovalDate.HasValue &&
                   string.IsNullOrWhiteSpace(CreationUserContains) &&
                   string.IsNullOrWhiteSpace(PlanApproverContains) &&
                   string.IsNullOrWhiteSpace(TreatmentApproverContains) &&
                   string.IsNullOrWhiteSpace(ImageOrientationContains) &&
                   !IsDoseValid.HasValue &&
                   !AutoCrop.HasValue &&
                   !HasDVHEstimates.HasValue &&
                   !HasTreatment.HasValue;
        }
    }
}

