using System;
using System.Collections.Generic;
using System.Linq;
using ESAPIPatientBrowser.Models;
using VMS.TPS.Common.Model.API;
using ESAPIApp = VMS.TPS.Common.Model.API.Application;

namespace ESAPIPatientBrowser.Services
{
    public class PatientSearchService
    {
        private readonly ESAPIApp _application;

        public PatientSearchService(ESAPIApp application)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
        }

        /// <summary>
        /// Gets smart search suggestions for real-time patient lookup (similar to PlanScoreCard)
        /// Safe version that doesn't open patients to avoid atomic access violations
        /// </summary>
        public List<PatientInfo> GetSmartSearchSuggestions(string searchText, int maxResults = 10)
        {
            var results = new List<PatientInfo>();

            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
                return results;

            try
            {
                // Get patient summaries from Eclipse - this is safe as it doesn't open patients
                var patientSummaries = _application.PatientSummaries;

                // Apply smart matching algorithm (similar to PlanScoreCard's SmartSearchService)
                var filteredSummaries = patientSummaries.AsEnumerable()
                    .Where(p => IsSmartMatch(p, searchText))
                    .OrderByDescending(p => GetMatchScore(p, searchText))
                    .ThenByDescending(p => p.CreationDateTime)
                    .Take(maxResults);

                // Convert to PatientInfo objects WITHOUT loading plans or opening patients
                // This is safe as we only use PatientSummary data
                foreach (var summary in filteredSummaries)
                {
                    try
                    {
                        var patientInfo = new PatientInfo
                        {
                            PatientId = summary.Id,
                            FirstName = summary.FirstName,
                            LastName = summary.LastName,
                            DateOfBirth = summary.DateOfBirth,
                            CreationDate = summary.CreationDateTime,
                            // Don't load plans in suggestions - they will be loaded when patient is selected
                            Plans = new List<PlanInfo>()
                        };
                        results.Add(patientInfo);
                    }
                    catch (Exception)
                    {
                        // Skip patients that can't be accessed safely
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                // Return empty results on error for suggestions
            }

            return results;
        }

        /// <summary>
        /// Searches for patients by various criteria
        /// </summary>
        public List<PatientInfo> SearchPatients(string searchText = "", DateTime? fromDate = null, DateTime? toDate = null, int? maxResults = null)
        {
            var results = new List<PatientInfo>();

            try
            {
                // Get patient summaries from Eclipse
                var patientSummaries = _application.PatientSummaries;

                // Apply date filter first for performance
                var filteredSummaries = patientSummaries.AsEnumerable();
                
                if (fromDate.HasValue)
                {
                    filteredSummaries = filteredSummaries.Where(p => p.CreationDateTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    filteredSummaries = filteredSummaries.Where(p => p.CreationDateTime <= toDate.Value);
                }

                // Apply text search filter
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchTerms = searchText.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    filteredSummaries = filteredSummaries.Where(p => MatchesSearchTerms(p, searchTerms));
                }

                // Order by creation date (most recent first)
                filteredSummaries = filteredSummaries.OrderByDescending(p => p.CreationDateTime);
                
                // Apply limit only if specified
                if (maxResults.HasValue)
                {
                    filteredSummaries = filteredSummaries.Take(maxResults.Value);
                }

                // Convert to PatientInfo objects and load plans
                foreach (var summary in filteredSummaries)
                {
                    var patientInfo = CreatePatientInfo(summary);
                    if (patientInfo != null)
                    {
                        results.Add(patientInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error searching patients: {ex.Message}", ex);
            }

            return results;
        }

        /// <summary>
        /// Gets basic patient information WITHOUT loading plans (for cumulative list building)
        /// Enhanced with atomic access violation protection
        /// </summary>
        public PatientInfo GetPatientBasicInfo(string patientId)
        {
            Patient patient = null;
            try
            {
                // AOS pattern: First check if patient exists
                if (!DoesPatientExist(patientId))
                {
                    System.Diagnostics.Debug.WriteLine($"Patient {patientId} does not exist");
                    return null;
                }

                // AOS pattern: Always close patient before opening (critical!)
                _application.ClosePatient();
                
                // Open the patient
                patient = _application.OpenPatientById(patientId);
                if (patient == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open patient {patientId}");
                    return null;
                }

                var patientInfo = new PatientInfo
                {
                    PatientId = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    DateOfBirth = patient.DateOfBirth,
                    // Don't load plans here - they'll be loaded on demand
                    Plans = new List<PlanInfo>()
                };

                return patientInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading patient basic info for {patientId}: {ex.Message}");
                return null;
            }
            finally
            {
                // AOS pattern: Always close patient when done
                try
                {
                    _application.ClosePatient();
                }
                catch (Exception)
                {
                    // Ignore close errors
                }
            }
        }

        /// <summary>
        /// Gets detailed patient information including all plans
        /// Safe version using AOS-MachineSwitch pattern with existence check first
        /// </summary>
        public PatientInfo GetPatientDetails(string patientId)
        {
            Patient patient = null;
            try
            {
                // AOS pattern: First check if patient exists
                if (!DoesPatientExist(patientId))
                {
                    System.Diagnostics.Debug.WriteLine($"Patient {patientId} does not exist");
                    return null;
                }

                // AOS pattern: Always close patient before opening (critical!)
                _application.ClosePatient();
                
                // Open the patient
                patient = _application.OpenPatientById(patientId);
                if (patient == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open patient {patientId}");
                    return null;
                }

                var patientInfo = new PatientInfo
                {
                    PatientId = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    DateOfBirth = patient.DateOfBirth,
                    Plans = LoadPatientPlans(patient)
                };

                return patientInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading patient details for {patientId}: {ex.Message}");
                return null;
            }
            finally
            {
                // AOS pattern: Always close patient when done
                try
                {
                    _application.ClosePatient();
                }
                catch (Exception)
                {
                    // Ignore close errors
                }
            }
        }

        private PatientInfo CreatePatientInfo(PatientSummary summary)
        {
            try
            {
                return new PatientInfo
                {
                    PatientId = summary.Id,
                    FirstName = summary.FirstName,
                    LastName = summary.LastName,
                    DateOfBirth = summary.DateOfBirth,
                    CreationDate = summary.CreationDateTime,
                    // Plans will be loaded on-demand when patient is selected
                };
            }
            catch (Exception)
            {
                // Skip patients that can't be loaded
                return null;
            }
        }

        private List<PlanInfo> LoadPatientPlans(Patient patient)
        {
            var plans = new List<PlanInfo>();

            try
            {
                // Check if patient is valid
                if (patient == null || patient.Courses == null)
                    return plans;

                foreach (var course in patient.Courses)
                {
                    try
                    {
                        // Check if course is valid and has plan setups
                        if (course?.PlanSetups == null)
                            continue;

                        foreach (var planSetup in course.PlanSetups)
                        {
                            try
                            {
                                // Only include plans with structure sets to avoid accessing incomplete data
                                if (planSetup?.StructureSet == null)
                                    continue;

                                var planInfo = new PlanInfo
                                {
                                    PatientId = patient.Id,
                                    CourseId = course.Id,
                                    PlanId = planSetup.Id,
                                    CreationDate = planSetup.CreationDateTime,
                                    CreationUser = planSetup.CreationUserName?.Replace('\\', '_'),
                                    ApprovalStatus = planSetup.ApprovalStatus.ToString(),
                                    TotalDose = GetTotalDoseValue(planSetup),
                                    NumberOfFractions = planSetup.NumberOfFractions ?? 0,
                                    StructureSetId = planSetup.StructureSet?.Id ?? "N/A"
                                };

                                plans.Add(planInfo);
                            }
                            catch (Exception ex)
                            {
                                // Log but skip plans that can't be loaded
                                System.Diagnostics.Debug.WriteLine($"Error loading plan {planSetup?.Id}: {ex.Message}");
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but skip courses that can't be loaded
                        System.Diagnostics.Debug.WriteLine($"Error loading course {course?.Id}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but return what we have so far
                System.Diagnostics.Debug.WriteLine($"Error loading patient plans: {ex.Message}");
            }

            return plans.OrderByDescending(p => p.CreationDate).ToList();
        }

        private double GetTotalDoseValue(PlanSetup planSetup)
        {
            try
            {
                // Check if TotalDose is null or has no value
                if (planSetup.TotalDose == null)
                    return 0.0;

                // In ESAPI, DoseValue has a Dose property and a Unit property
                // Convert to Gy if needed
                var dose = planSetup.TotalDose.Dose;
                var unit = planSetup.TotalDose.Unit;

                if (unit == VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.cGy)
                {
                    return dose / 100.0; // Convert cGy to Gy
                }
                else if (unit == VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy)
                {
                    return dose;
                }
                else
                {
                    return dose; // Return as-is for other units
                }
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        private bool MatchesSearchTerms(PatientSummary patient, string[] searchTerms)
        {
            if (searchTerms.Length == 0) return true;

            var searchableText = $"{patient.Id} {patient.FirstName} {patient.LastName}".ToUpper();

            if (searchTerms.Length == 1)
            {
                // Single term - check if it matches any field
                return searchableText.Contains(searchTerms[0].ToUpper());
            }
            else
            {
                // Multiple terms - check for last name, first name pattern or first name, last name pattern
                var term1 = searchTerms[0].ToUpper();
                var term2 = searchTerms[1].ToUpper();

                return (patient.LastName.ToUpper().Contains(term1) && patient.FirstName.ToUpper().Contains(term2)) ||
                       (patient.FirstName.ToUpper().Contains(term1) && patient.LastName.ToUpper().Contains(term2)) ||
                       patient.Id.ToUpper().Contains(term1);
            }
        }

        #region Patient Existence Methods (AOS-MachineSwitch pattern)

        /// <summary>
        /// Safely checks if a patient exists without opening them (AOS-MachineSwitch pattern)
        /// Simple and direct like AOS - no locks or delays needed
        /// </summary>
        public bool DoesPatientExist(string patientId)
        {
            if (_application == null)
                return false;

            if (string.IsNullOrEmpty(patientId))
                return false;

            try
            {
                // AOS pattern: direct access to PatientSummaries, simple and clean
                return _application.PatientSummaries.Count(x => x.Id.Equals(patientId, StringComparison.OrdinalIgnoreCase)) > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking patient existence for {patientId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets filtered patient IDs for real-time dropdown (AOS-MachineSwitch pattern)
        /// </summary>
        public List<string> GetFilteredPatientIds(string searchText, int maxResults = 50)
        {
            if (string.IsNullOrEmpty(searchText))
                return new List<string>();

            try
            {
                // AOS pattern: direct access, simple and clean
                return _application.PatientSummaries
                    .Select(x => x.Id)
                    .Where(patientId => patientId.ToLower().Contains(searchText.ToLower()))
                    .OrderBy(x => x)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering patient IDs: {ex.Message}");
                return new List<string>();
            }
        }

        #endregion

        #region Smart Search Methods (PlanScoreCard-style)

        private bool IsSmartMatch(PatientSummary patient, string searchText)
        {
            var searchTerms = GetSearchTerms(searchText);

            if (searchTerms.Length == 0) return false;

            if (searchTerms.Length == 1)
            {
                // Single term - check if it matches any field
                return IsMatch(patient.Id, searchTerms[0]) ||
                       IsMatch(patient.LastName, searchTerms[0]) ||
                       IsMatch(patient.FirstName, searchTerms[0]);
            }
            else
            {
                // Multiple terms - check for last name, first name pattern or first name, last name pattern
                return IsMatchWithLastThenFirstName(patient, searchTerms) ||
                       IsMatchWithFirstThenLastName(patient, searchTerms);
            }
        }

        private int GetMatchScore(PatientSummary patient, string searchText)
        {
            var searchTerms = GetSearchTerms(searchText);
            int score = 0;

            foreach (var term in searchTerms)
            {
                // Exact matches get higher scores (all case-insensitive)
                if (patient.Id.Equals(term, StringComparison.OrdinalIgnoreCase))
                    score += 100;
                else if (patient.Id.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                    score += 80;
                else if (patient.Id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 60;

                if (patient.LastName.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                    score += 70;
                else if (patient.LastName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 50;

                if (patient.FirstName.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                    score += 70;
                else if (patient.FirstName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 50;
            }

            return score;
        }

        private string[] GetSearchTerms(string searchText)
        {
            // Split by whitespace and remove any separators (similar to PlanScoreCard)
            return searchText.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .ToArray();
        }

        private bool IsMatch(string actual, string candidate)
        {
            return actual.ToUpper().Contains(candidate.ToUpper());
        }

        private bool IsMatchWithLastThenFirstName(PatientSummary patient, string[] searchTerms)
        {
            return IsMatch(patient.LastName, searchTerms[0]) &&
                   IsMatch(patient.FirstName, searchTerms[1]);
        }

        private bool IsMatchWithFirstThenLastName(PatientSummary patient, string[] searchTerms)
        {
            return IsMatch(patient.FirstName, searchTerms[0]) &&
                   IsMatch(patient.LastName, searchTerms[1]);
        }

        #endregion
    }
}
