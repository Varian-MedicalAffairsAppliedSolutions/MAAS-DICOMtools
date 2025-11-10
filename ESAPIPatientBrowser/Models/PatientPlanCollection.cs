using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ESAPIPatientBrowser.Models
{
    public class PatientPlanCollection
    {
        [JsonProperty("exportDateTime")]
        public DateTime ExportDateTime { get; set; } = DateTime.Now;

        [JsonProperty("totalPatients")]
        public int TotalPatients => Patients?.Count ?? 0;

        [JsonProperty("totalPlans")]
        public int TotalPlans => Patients?.SelectMany(p => p.Plans).Count() ?? 0;

        [JsonProperty("patients")]
        public List<PatientInfo> Patients { get; set; } = new List<PatientInfo>();

        /// <summary>
        /// Indicates whether optimization objectives were exported
        /// </summary>
        [JsonProperty("hasObjectives")]
        public bool HasObjectives { get; set; } = false;

        /// <summary>
        /// Relative path to objectives staging folder (e.g., "Objectives")
        /// Stored as relative path for portability - resolved relative to JSON file location
        /// </summary>
        [JsonProperty("objectivesStagingPath")]
        public string ObjectivesStagingPath { get; set; }

        /// <summary>
        /// Mappings of objective files to patients/plans
        /// Critical for handling anonymization - maps original patient IDs to objective filenames
        /// </summary>
        [JsonProperty("objectiveFiles")]
        public List<ObjectiveFileMapping> ObjectiveFiles { get; set; } = new List<ObjectiveFileMapping>();

        /// <summary>
        /// Gets a flattened list of Patient IDs for DICOM tools
        /// </summary>
        [JsonIgnore]
        public List<string> PatientIds => Patients?.Where(p => p.IsSelected)?.Select(p => p.PatientId).ToList() ?? new List<string>();

        /// <summary>
        /// Gets a formatted string of Patient IDs for batch processing
        /// </summary>
        [JsonIgnore]
        public string PatientIdString => string.Join(";", PatientIds);

        public static PatientPlanCollection FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<PatientPlanCollection>(json);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}", ex);
            }
        }

        public string ToJson(bool formatted = true)
        {
            return JsonConvert.SerializeObject(this, formatted ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Creates a summary string for display
        /// </summary>
        public string GetSummary()
        {
            var selectedPatients = Patients?.Where(p => p.IsSelected).ToList() ?? new List<PatientInfo>();
            var selectedPlans = selectedPatients.SelectMany(p => p.Plans.Where(pl => pl.IsSelected)).ToList();

            return $"Selected: {selectedPatients.Count} patients, {selectedPlans.Count} plans";
        }
    }

    /// <summary>
    /// Maps optimization objective files to their corresponding patient/plan
    /// Used by batch files to copy objective files to correct (possibly anonymized) patient folders
    /// </summary>
    public class ObjectiveFileMapping
    {
        [JsonProperty("patientId")]
        public string PatientId { get; set; }  // Original patient ID (before anonymization)

        [JsonProperty("courseId")]
        public string CourseId { get; set; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }  // e.g., "Patient1_Plan1_objectives.json"
    }
}
