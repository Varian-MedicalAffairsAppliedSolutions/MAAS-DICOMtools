using System;
using System.Collections.Generic;
using System.Linq;
using ESAPIPatientBrowser.Models;
using VMS.TPS.Common.Model.API;

namespace ESAPIPatientBrowser.Services
{
    // Extension method for case-insensitive Contains (not available in .NET Framework 4.8)
    internal static class StringExtensions
    {
        public static bool ContainsIgnoreCase(this string source, string value)
        {
            if (source == null || value == null) return false;
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public class AdvancedSearchService
    {
        private readonly Application _esapiApp;

        public AdvancedSearchService(Application esapiApp)
        {
            _esapiApp = esapiApp ?? throw new ArgumentNullException(nameof(esapiApp));
        }

        /// <summary>
        /// Performs an advanced search across patients, examining plan-level details
        /// </summary>
        public List<PatientInfo> PerformAdvancedSearch(DateTime fromDate, DateTime toDate, AdvancedSearchCriteria criteria, IProgress<string> progress)
        {
            var results = new List<PatientInfo>();
            
            // Get patients in date range
            var patientSummaries = _esapiApp.PatientSummaries
                .Where(ps => ps.CreationDateTime >= fromDate && ps.CreationDateTime <= toDate)
                .OrderBy(ps => ps.Id)
                .ToList();

            int total = patientSummaries.Count;
            int current = 0;

            foreach (var summary in patientSummaries)
            {
                current++;
                progress?.Report($"Scanning patient {current}/{total}: {summary.Id}");

                try
                {
                    var patient = _esapiApp.OpenPatient(summary);
                    if (patient == null) continue;

                    var patientInfo = new PatientInfo
                    {
                        PatientId = patient.Id,
                        FirstName = patient.FirstName,
                        LastName = patient.LastName,
                        DateOfBirth = patient.DateOfBirth,
                        CreationDate = summary.CreationDateTime,
                        Plans = new List<PlanInfo>()
                    };

                    bool patientHasMatchingPlans = false;

                    // Scan all courses and plans
                    foreach (var course in patient.Courses)
                    {
                        foreach (var planSetup in course.PlanSetups)
                        {
                            var planInfo = ExtractDetailedPlanInfo(planSetup);
                            
                            // Check if plan matches criteria
                            if (PlanMatchesCriteria(planInfo, criteria))
                            {
                                patientInfo.Plans.Add(planInfo);
                                patientHasMatchingPlans = true;
                            }
                        }
                    }

                    // Only add patient if they have matching plans
                    if (patientHasMatchingPlans)
                    {
                        results.Add(patientInfo);
                    }

                    _esapiApp.ClosePatient();
                }
                catch (Exception ex)
                {
                    progress?.Report($"Error scanning patient {summary.Id}: {ex.Message}");
                }
            }

            progress?.Report($"Advanced search complete. Found {results.Count} patients with matching plans.");
            return results;
        }

        /// <summary>
        /// Extracts detailed plan information similar to PlanMining
        /// </summary>
        public PlanInfo ExtractDetailedPlanInfo(PlanSetup planSetup)
        {
            var planInfo = new PlanInfo
            {
                PatientId = planSetup.Course.Patient.Id,
                CourseId = planSetup.Course.Id,
                PlanId = planSetup.Id,
                CreationDate = planSetup.CreationDateTime,
                ApprovalStatus = planSetup.ApprovalStatus.ToString(),
                NumberOfFractions = planSetup.NumberOfFractions ?? 0
            };

            // Patient demographics
            try
            {
                planInfo.PatientSex = planSetup.Course.Patient.Sex;
                planInfo.Hospital = planSetup.Course.Patient.Hospital?.Id;
            }
            catch { }

            // Course intent
            try
            {
                planInfo.CourseIntent = planSetup.Course.Intent;
            }
            catch { }

            // Total dose - normalize to Gy for consistent filtering
            try
            {
                if (planSetup.TotalDose != null)
                {
                    planInfo.TotalDose = ConvertDoseToGy(planSetup.TotalDose);
                }
            }
            catch { }

            // Dose per fraction - normalize to Gy for consistent filtering
            try
            {
                if (planSetup.DosePerFraction != null)
                {
                    planInfo.DosePerFraction = ConvertDoseToGy(planSetup.DosePerFraction);
                }
            }
            catch { }

            // Structure set
            try
            {
                if (planSetup.StructureSet != null)
                {
                    planInfo.StructureSetId = planSetup.StructureSet.Id;
                }
            }
            catch { }

            // Target volume (PTV)
            try
            {
                planInfo.TargetVolumeId = planSetup.TargetVolumeID;
            }
            catch { }

            // Prescription details
            try
            {
                if (planSetup.RTPrescription != null)
                {
                    planInfo.PrescriptionSite = planSetup.RTPrescription.Site;
                    planInfo.PrescriptionPhase = planSetup.RTPrescription.PhaseType;
                    planInfo.PrescriptionTechnique = planSetup.RTPrescription.Technique;
                }
            }
            catch { }

            // Plan intent
            try
            {
                planInfo.PlanIntent = planSetup.PlanIntent;
            }
            catch { }

            // Machine and energy info
            try
            {
                if (planSetup.Beams.Any(b => !b.IsSetupField))
                {
                    var firstBeam = planSetup.Beams.First(b => !b.IsSetupField);
                    planInfo.MachineId = firstBeam.TreatmentUnit?.Id;
                    planInfo.Energy = firstBeam.EnergyModeDisplayName;
                    planInfo.BeamTechnique = firstBeam.Technique?.ToString();
                    
                    // MLC info
                    if (firstBeam.MLC != null)
                    {
                        planInfo.MlcId = firstBeam.MLC.Id;
                    }
                    planInfo.MlcType = firstBeam.MLCPlanType.ToString();
                    
                    // Control points (from first non-setup beam)
                    planInfo.NumberOfControlPoints = firstBeam.ControlPoints?.Count ?? 0;
                }
            }
            catch { }

            // Calculation model
            try
            {
                planInfo.PhotonCalculationModel = planSetup.PhotonCalculationModel;
            }
            catch { }

            // Auto crop (from optimization logs)
            try
            {
                if (planSetup.Beams.Any(b => !b.IsSetupField))
                {
                    var beam = planSetup.Beams.First(b => !b.IsSetupField);
                    var messages = beam.CalculationLogs?.Where(x => x.Category.ToUpper().Contains("OPTIMIZATION"));
                    if (messages != null)
                    {
                        foreach (var m in messages)
                        {
                            var searchString = "Automatic target overlap control";
                            var mLac = m.MessageLines?.FirstOrDefault(x => x.ToUpper().Contains(searchString.ToUpper()));
                            if (!string.IsNullOrEmpty(mLac))
                            {
                                planInfo.AutoCrop = !mLac.ToUpper().Contains("OFF");
                            }
                        }
                    }
                }
            }
            catch { }

            // Creation user
            try
            {
                planInfo.CreationUser = planSetup.CreationUserName?.Replace('\\', '_');
            }
            catch { }

            // Approvers
            try
            {
                planInfo.PlanApprover = planSetup.PlanningApprover?.Replace('\\', '_');
                planInfo.TreatmentApprover = planSetup.TreatmentApprover?.Replace('\\', '_');
            }
            catch { }

            // Approval date
            try
            {
                if (planSetup.ApprovalHistory.Any())
                {
                    planInfo.ApprovalDate = planSetup.ApprovalHistory
                        .OrderByDescending(ah => ah.ApprovalDateTime)
                        .First().ApprovalDateTime;
                }
            }
            catch { }

            // Image orientation
            try
            {
                if (planSetup.StructureSet?.Image != null)
                {
                    planInfo.ImageOrientation = planSetup.StructureSet.Image.ImagingOrientation.ToString();
                }
            }
            catch { }

            // Dose valid
            try
            {
                planInfo.IsDoseValid = planSetup.IsDoseValid;
            }
            catch { }

            // DVH Estimates
            try
            {
                planInfo.HasDVHEstimates = planSetup.DVHEstimates != null && planSetup.DVHEstimates.Any();
            }
            catch { }

            // Structure list
            try
            {
                if (planSetup.StructureSet != null)
                {
                    planInfo.StructureIds = planSetup.StructureSet.Structures
                        .Select(s => s.Id)
                        .ToList();
                }
            }
            catch { }

            // Treatment sessions (delivered fractions)
            try
            {
                if (planSetup.TreatmentSessions != null)
                {
                    planInfo.DeliveredFractions = planSetup.TreatmentSessions.Count();
                }
            }
            catch { }

            return planInfo;
        }

        /// <summary>
        /// Checks if a plan matches the search criteria
        /// </summary>
        public bool PlanMatchesCriteria(PlanInfo plan, AdvancedSearchCriteria criteria)
        {
            // If all criteria are empty, match everything
            if (criteria.IsEmpty())
            {
                System.Diagnostics.Debug.WriteLine($"  Plan {plan.PlanId}: Criteria is empty, matching all");
                return true;
            }

            // Target volume check (case-insensitive contains)
            if (!string.IsNullOrWhiteSpace(criteria.TargetVolumeContains))
            {
                if (string.IsNullOrWhiteSpace(plan.TargetVolumeId) ||
                    !plan.TargetVolumeId.ContainsIgnoreCase(criteria.TargetVolumeContains))
                {
                    System.Diagnostics.Debug.WriteLine($"  Plan {plan.PlanId}: Failed target volume check. Looking for '{criteria.TargetVolumeContains}', found '{plan.TargetVolumeId}'");
                    return false;
                }
            }

            // Prescription site check
            if (!string.IsNullOrWhiteSpace(criteria.PrescriptionSiteContains))
            {
                if (string.IsNullOrWhiteSpace(plan.PrescriptionSite) ||
                    !plan.PrescriptionSite.ContainsIgnoreCase(criteria.PrescriptionSiteContains))
                {
                    return false;
                }
            }

            // Prescription technique check
            if (!string.IsNullOrWhiteSpace(criteria.PrescriptionTechniqueContains))
            {
                if (string.IsNullOrWhiteSpace(plan.PrescriptionTechnique) ||
                    !plan.PrescriptionTechnique.ContainsIgnoreCase(criteria.PrescriptionTechniqueContains))
                {
                    return false;
                }
            }

            // Plan name check
            if (!string.IsNullOrWhiteSpace(criteria.PlanNameContains))
            {
                if (string.IsNullOrWhiteSpace(plan.PlanId) ||
                    !plan.PlanId.ContainsIgnoreCase(criteria.PlanNameContains))
                {
                    return false;
                }
            }

            // Structure name check (searches all structures in the plan)
            if (!string.IsNullOrWhiteSpace(criteria.StructureNameContains))
            {
                if (plan.StructureIds == null || !plan.StructureIds.Any() ||
                    !plan.StructureIds.Any(s => s.ContainsIgnoreCase(criteria.StructureNameContains)))
                {
                    return false;
                }
            }

            // Machine ID check
            if (!string.IsNullOrWhiteSpace(criteria.MachineIdContains))
            {
                if (string.IsNullOrWhiteSpace(plan.MachineId) ||
                    !plan.MachineId.ContainsIgnoreCase(criteria.MachineIdContains))
                {
                    return false;
                }
            }

            // Energy check
            if (!string.IsNullOrWhiteSpace(criteria.EnergyContains))
            {
                if (string.IsNullOrWhiteSpace(plan.Energy) ||
                    !plan.Energy.ContainsIgnoreCase(criteria.EnergyContains))
                {
                    return false;
                }
            }

            // Dose range check
            if (criteria.MinDose.HasValue && plan.TotalDose < criteria.MinDose.Value)
            {
                return false;
            }

            if (criteria.MaxDose.HasValue && plan.TotalDose > criteria.MaxDose.Value)
            {
                return false;
            }

            // Plan intent check
            if (!string.IsNullOrWhiteSpace(criteria.PlanIntentContains))
            {
                if (string.IsNullOrWhiteSpace(plan.PlanIntent) ||
                    !plan.PlanIntent.ContainsIgnoreCase(criteria.PlanIntentContains))
                {
                    return false;
                }
            }

            // Approval status check
            if (!string.IsNullOrWhiteSpace(criteria.ApprovalStatusContains))
            {
                if (string.IsNullOrWhiteSpace(plan.ApprovalStatus) ||
                    !plan.ApprovalStatus.ContainsIgnoreCase(criteria.ApprovalStatusContains))
                {
                    return false;
                }
            }

            // ===== HIGH PRIORITY FILTERS =====
            
            // Number of fractions range
            if (criteria.MinFractions.HasValue && plan.NumberOfFractions < criteria.MinFractions.Value)
            {
                return false;
            }
            if (criteria.MaxFractions.HasValue && plan.NumberOfFractions > criteria.MaxFractions.Value)
            {
                return false;
            }

            // Dose per fraction range
            if (criteria.MinDosePerFraction.HasValue && plan.DosePerFraction < criteria.MinDosePerFraction.Value)
            {
                return false;
            }
            if (criteria.MaxDosePerFraction.HasValue && plan.DosePerFraction > criteria.MaxDosePerFraction.Value)
            {
                return false;
            }

            // MLC Type
            if (!string.IsNullOrWhiteSpace(criteria.MlcTypeContains))
            {
                if (string.IsNullOrWhiteSpace(plan.MlcType) ||
                    !plan.MlcType.ContainsIgnoreCase(criteria.MlcTypeContains))
                {
                    return false;
                }
            }

            // Beam Technique
            if (!string.IsNullOrWhiteSpace(criteria.BeamTechniqueContains))
            {
                if (string.IsNullOrWhiteSpace(plan.BeamTechnique) ||
                    !plan.BeamTechnique.ContainsIgnoreCase(criteria.BeamTechniqueContains))
                {
                    return false;
                }
            }

            // Number of control points range
            if (criteria.MinControlPoints.HasValue && plan.NumberOfControlPoints < criteria.MinControlPoints.Value)
            {
                return false;
            }
            if (criteria.MaxControlPoints.HasValue && plan.NumberOfControlPoints > criteria.MaxControlPoints.Value)
            {
                return false;
            }

            // Patient Sex
            if (!string.IsNullOrWhiteSpace(criteria.PatientSexContains))
            {
                if (string.IsNullOrWhiteSpace(plan.PatientSex) ||
                    !plan.PatientSex.ContainsIgnoreCase(criteria.PatientSexContains))
                {
                    return false;
                }
            }

            // ===== MEDIUM PRIORITY FILTERS =====

            // Hospital
            if (!string.IsNullOrWhiteSpace(criteria.HospitalContains))
            {
                if (string.IsNullOrWhiteSpace(plan.Hospital) ||
                    !plan.Hospital.ContainsIgnoreCase(criteria.HospitalContains))
                {
                    return false;
                }
            }

            // Course Intent
            if (!string.IsNullOrWhiteSpace(criteria.CourseIntentContains))
            {
                if (string.IsNullOrWhiteSpace(plan.CourseIntent) ||
                    !plan.CourseIntent.ContainsIgnoreCase(criteria.CourseIntentContains))
                {
                    return false;
                }
            }

            // MLC ID
            if (!string.IsNullOrWhiteSpace(criteria.MlcIdContains))
            {
                if (string.IsNullOrWhiteSpace(plan.MlcId) ||
                    !plan.MlcId.ContainsIgnoreCase(criteria.MlcIdContains))
                {
                    return false;
                }
            }

            // Approval date range
            if (criteria.MinApprovalDate.HasValue && (!plan.ApprovalDate.HasValue || plan.ApprovalDate.Value < criteria.MinApprovalDate.Value))
            {
                return false;
            }
            if (criteria.MaxApprovalDate.HasValue && (!plan.ApprovalDate.HasValue || plan.ApprovalDate.Value > criteria.MaxApprovalDate.Value))
            {
                return false;
            }

            // Creation User
            if (!string.IsNullOrWhiteSpace(criteria.CreationUserContains))
            {
                if (string.IsNullOrWhiteSpace(plan.CreationUser) ||
                    !plan.CreationUser.ContainsIgnoreCase(criteria.CreationUserContains))
                {
                    return false;
                }
            }

            // Plan Approver
            if (!string.IsNullOrWhiteSpace(criteria.PlanApproverContains))
            {
                if (string.IsNullOrWhiteSpace(plan.PlanApprover) ||
                    !plan.PlanApprover.ContainsIgnoreCase(criteria.PlanApproverContains))
                {
                    return false;
                }
            }

            // Treatment Approver
            if (!string.IsNullOrWhiteSpace(criteria.TreatmentApproverContains))
            {
                if (string.IsNullOrWhiteSpace(plan.TreatmentApprover) ||
                    !plan.TreatmentApprover.ContainsIgnoreCase(criteria.TreatmentApproverContains))
                {
                    return false;
                }
            }

            // ===== LOWER PRIORITY FILTERS =====

            // Image Orientation
            if (!string.IsNullOrWhiteSpace(criteria.ImageOrientationContains))
            {
                if (string.IsNullOrWhiteSpace(plan.ImageOrientation) ||
                    !plan.ImageOrientation.ContainsIgnoreCase(criteria.ImageOrientationContains))
                {
                    return false;
                }
            }

            // IsDoseValid
            if (criteria.IsDoseValid.HasValue)
            {
                if (!plan.IsDoseValid.HasValue || plan.IsDoseValid.Value != criteria.IsDoseValid.Value)
                {
                    return false;
                }
            }

            // Auto Crop
            if (criteria.AutoCrop.HasValue)
            {
                if (!plan.AutoCrop.HasValue || plan.AutoCrop.Value != criteria.AutoCrop.Value)
                {
                    return false;
                }
            }

            // Has DVH Estimates
            if (criteria.HasDVHEstimates.HasValue)
            {
                if (!plan.HasDVHEstimates.HasValue || plan.HasDVHEstimates.Value != criteria.HasDVHEstimates.Value)
                {
                    return false;
                }
            }

            // Has Treatment (at least one delivered fraction)
            if (criteria.HasTreatment.HasValue && criteria.HasTreatment.Value)
            {
                if (plan.DeliveredFractions == 0)
                {
                    return false;
                }
            }

            System.Diagnostics.Debug.WriteLine($"  Plan {plan.PlanId}: ✓ MATCHED all criteria");
            return true;
        }

        /// <summary>
        /// Converts a DoseValue to Gy for consistent filtering across different systems
        /// </summary>
        private double ConvertDoseToGy(VMS.TPS.Common.Model.Types.DoseValue doseValue)
        {
            if (doseValue == null)
                return 0.0;

            // Check the unit and convert to Gy
            switch (doseValue.Unit)
            {
                case VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy:
                    return doseValue.Dose;
                
                case VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.cGy:
                    return doseValue.Dose / 100.0; // Convert cGy to Gy
                
                case VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Unknown:
                default:
                    // Assume Gy if unknown (most common)
                    return doseValue.Dose;
            }
        }
    }
}

