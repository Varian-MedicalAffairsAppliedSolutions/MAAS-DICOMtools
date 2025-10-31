using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using ESAPIPatientBrowser.Models;

namespace ESAPIPatientBrowser.Services
{
    /// <summary>
    /// Service for exporting optimization objectives from Eclipse plans to JSON format
    /// </summary>
    public class OptimizationObjectivesExportService
    {
        /// <summary>
        /// Export optimization objectives for all plans to a staging folder
        /// Returns tuple: (staging path, file mappings for batch file copy operations, list of skipped plans)
        /// </summary>
        public (string StagingPath, List<ObjectiveFileMapping> FileMappings, List<SkippedPlanInfo> SkippedPlans) ExportObjectivesToStagingFolder(
            Application esapiApp,
            PatientPlanCollection collection,
            string baseExportPath)
        {
            // Create staging folder for objectives
            string stagingFolder = Path.Combine(baseExportPath, "Objectives");
            
            try
            {
                if (!Directory.Exists(stagingFolder))
                    Directory.CreateDirectory(stagingFolder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create staging folder: {ex.Message}");
                return (null, null, null);
            }

            var fileMappings = new List<ObjectiveFileMapping>();
            var skippedPlans = new List<SkippedPlanInfo>();
            int exportedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;

            foreach (var patientPlan in collection.Patients)
            {
                Patient patient = null;
                try
                {
                    // Close any open patient first (critical for avoiding access violations)
                    try { esapiApp.ClosePatient(); } catch { }

                    patient = esapiApp.OpenPatientById(patientPlan.PatientId);
                    if (patient == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to open patient: {patientPlan.PatientId}");
                        failedCount++;
                        continue;
                    }

                    foreach (var planSelection in patientPlan.Plans)
                    {
                        try
                        {
                            var course = patient.Courses.FirstOrDefault(c => c.Id == planSelection.CourseId);
                            if (course == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Course not found: {planSelection.CourseId} for patient {patientPlan.PatientId}");
                                failedCount++;
                                continue;
                            }

                            var planSetup = course.PlanSetups.FirstOrDefault(p => p.Id == planSelection.PlanId);
                            if (planSetup == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Plan not found: {planSelection.PlanId} in course {planSelection.CourseId}");
                                failedCount++;
                                continue;
                            }

                            // Check if plan has optimization setup
                            if (planSetup.OptimizationSetup == null ||
                                planSetup.OptimizationSetup.Objectives == null ||
                                !planSetup.OptimizationSetup.Objectives.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"No optimization objectives for: {planSelection.PlanId} (may be non-IMRT plan)");
                                skippedPlans.Add(new SkippedPlanInfo
                                {
                                    PatientId = patientPlan.PatientId,
                                    CourseId = planSelection.CourseId,
                                    PlanId = planSelection.PlanId,
                                    Reason = "No optimization objectives (may be non-IMRT plan)"
                                });
                                skippedCount++;
                                continue;
                            }

                            // Generate safe filename
                            string safePatientId = SanitizeFileName(patientPlan.PatientId);
                            string safePlanId = SanitizeFileName(planSelection.PlanId);
                            string filename = $"{safePatientId}_{safePlanId}_objectives.json";
                            string fullPath = Path.Combine(stagingFolder, filename);

                            // Export objectives to JSON
                            ExportObjectivesToJson(planSetup, patientPlan.PatientId, planSelection.CourseId, fullPath);

                            // Track the mapping (critical for anonymization support)
                            fileMappings.Add(new ObjectiveFileMapping
                            {
                                PatientId = patientPlan.PatientId,      // Original patient ID (before anonymization)
                                CourseId = planSelection.CourseId,
                                PlanId = planSelection.PlanId,
                                FileName = filename
                            });

                            exportedCount++;
                            System.Diagnostics.Debug.WriteLine($"Exported objectives for {patientPlan.PatientId}/{planSelection.PlanId}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error exporting objectives for plan {planSelection.PlanId}: {ex.Message}");
                            failedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing patient {patientPlan.PatientId}: {ex.Message}");
                    failedCount++;
                }
                finally
                {
                    // Always close patient to avoid access violations
                    try { esapiApp?.ClosePatient(); } catch { }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Objectives export summary: {exportedCount} exported, {skippedCount} skipped (no objectives), {failedCount} failed");

            return exportedCount > 0 ? (stagingFolder, fileMappings, skippedPlans) : (null, null, skippedPlans);
        }

        /// <summary>
        /// Export optimization objectives to JSON file
        /// Uses same format as standalone ExportObjectives.exe for consistency
        /// </summary>
        private void ExportObjectivesToJson(PlanSetup planSetup, string patientId, string courseId, string outputPath)
        {
            var objectivesDict = new Dictionary<string, List<ObjectiveDto>>();

            foreach (var obj in planSetup.OptimizationSetup.Objectives)
            {
                if (obj is OptimizationPointObjective pointObj)
                {
                    AddToDict(objectivesDict, pointObj.StructureId, new ObjectiveDto
                    {
                        StructureId = pointObj.StructureId,
                        Type = "Point",
                        Dose = Math.Round(pointObj.Dose.Dose, 1),
                        Volume = Math.Round(pointObj.Volume, 1),
                        Priority = pointObj.Priority
                    });
                }
                else if (obj is OptimizationLineObjective lineObj)
                {
                    AddToDict(objectivesDict, lineObj.StructureId, new ObjectiveDto
                    {
                        StructureId = lineObj.StructureId,
                        Type = "Line",
                        Dose = lineObj.CurveData.Select(pt => pt.DoseValue.Dose).ToList(),
                        Volume = lineObj.CurveData.Select(pt => pt.Volume).ToList(),
                        Priority = lineObj.Priority
                    });
                }
                else if (obj is OptimizationMeanDoseObjective meanObj)
                {
                    AddToDict(objectivesDict, meanObj.StructureId, new ObjectiveDto
                    {
                        StructureId = meanObj.StructureId,
                        Type = "MeanDose",
                        Dose = Math.Round(meanObj.Dose.Dose, 1),
                        Volume = -1d,
                        Priority = meanObj.Priority
                    });
                }
                else if (obj is OptimizationEUDObjective eudObj)
                {
                    AddToDict(objectivesDict, eudObj.StructureId, new ObjectiveDto
                    {
                        StructureId = eudObj.StructureId,
                        Type = "EUD",
                        Dose = Math.Round(eudObj.Dose.Dose, 1),
                        Volume = -1d,
                        Priority = eudObj.Priority,
                        ParameterA = Math.Round(eudObj.ParameterA, 2)
                    });
                }
            }

            // Build JSON manually for consistent formatting
            var sb = new StringBuilder();
            Action<int, string> add = (lvl, text) =>
            {
                sb.Append(new string(' ', lvl * 4));
                sb.Append(text);
                sb.Append('\n');
            };

            add(0, "{");
            add(1, "\"_meta\": {");
            add(2, $"\"PlanId\": \"{Escape(planSetup.Id)}\",");
            add(2, $"\"CourseId\": \"{Escape(courseId)}\",");
            add(2, $"\"PatientId\": \"{Escape(patientId)}\",");
            add(2, $"\"Created\": \"{Escape(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))}\",");
            add(2, $"\"NumberOfObjectives\": {planSetup.OptimizationSetup.Objectives.Count()},");
            add(2, $"\"NormalizedValue\": {planSetup.PlanNormalizationValue}");
            add(1, "},");
            add(1, "\"objectives\": {");

            int structIdx = 0;
            foreach (var kvp in objectivesDict)
            {
                add(2, $"\"{Escape(kvp.Key)}\": [");
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var o = kvp.Value[i];
                    add(3, "{");
                    add(4, $"\"StructureId\": \"{Escape(o.StructureId)}\",");
                    add(4, $"\"Type\": \"{Escape(o.Type)}\",");

                    var doseSb = new StringBuilder();
                    AppendValue(doseSb, o.Dose, 5);
                    add(4, $"\"Dose\": {doseSb.ToString().TrimEnd()},");

                    var volSb = new StringBuilder();
                    AppendValue(volSb, o.Volume, 5);
                    add(4, $"\"Volume\": {volSb.ToString().TrimEnd()},");

                    add(4, $"\"Priority\": {o.Priority}" + (o.ParameterA.HasValue ? "," : ""));
                    
                    if (o.ParameterA.HasValue)
                    {
                        add(4, $"\"ParameterA\": {o.ParameterA.Value.ToString(CultureInfo.InvariantCulture)}");
                    }
                    
                    add(3, i < kvp.Value.Count - 1 ? "}," : "}");
                }
                add(2, structIdx < objectivesDict.Count - 1 ? "] ," : "]");
                structIdx++;
            }

            add(1, "}");
            add(0, "}");

            File.WriteAllText(outputPath, sb.ToString());
        }

        #region Helper Classes and Methods

        private class ObjectiveDto
        {
            public string StructureId;
            public string Type;
            public object Dose;
            public object Volume;
            public double Priority;
            public double? ParameterA;  // For EUD objectives
        }

        private void AppendValue(StringBuilder sb, object value, int indent = 0)
        {
            if (value is IList<double> list)
            {
                sb.Append("[\n");
                for (int i = 0; i < list.Count; i++)
                {
                    sb.Append(new string(' ', indent * 4));
                    sb.Append(list[i].ToString(CultureInfo.InvariantCulture));
                    if (i < list.Count - 1) sb.Append(',');
                    sb.Append("\n");
                }
                sb.Append(new string(' ', (indent - 1 < 0 ? 0 : indent - 1) * 4));
                sb.Append("]");
            }
            else if (value is double d)
            {
                sb.Append(d.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append("null");
            }
        }

        private string Escape(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private void AddToDict(Dictionary<string, List<ObjectiveDto>> dict, string key, ObjectiveDto value)
        {
            if (!dict.ContainsKey(key))
                dict[key] = new List<ObjectiveDto>();
            dict[key].Add(value);
        }

        private string SanitizeFileName(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return "Unknown";

            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", filename.Split(invalidChars));
        }

        #endregion
    }

    /// <summary>
    /// Information about a plan that was skipped during objectives export
    /// </summary>
    public class SkippedPlanInfo
    {
        public string PatientId { get; set; }
        public string CourseId { get; set; }
        public string PlanId { get; set; }
        public string Reason { get; set; }
    }
}

