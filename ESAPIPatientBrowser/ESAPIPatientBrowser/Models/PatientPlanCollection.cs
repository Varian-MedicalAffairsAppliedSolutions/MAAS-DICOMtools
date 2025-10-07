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
}
