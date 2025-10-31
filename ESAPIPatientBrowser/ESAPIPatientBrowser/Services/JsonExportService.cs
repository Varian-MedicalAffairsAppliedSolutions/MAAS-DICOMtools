using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ESAPIPatientBrowser.Models;
using ESAPIPatientBrowser.Views;
using VMS.TPS.Common.Model.API;
using System.Collections.Generic;

namespace ESAPIPatientBrowser.Services
{
    public class JsonExportService
    {
        /// <summary>
        /// Exports patient/plan collection to JSON file with user file dialog
        /// Optionally includes optimization objectives export
        /// </summary>
        public bool ExportToFile(VMS.TPS.Common.Model.API.Application esapiApp, PatientPlanCollection collection, 
            bool includeObjectives, string defaultFileName = null)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Export Patient/Plan List",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = defaultFileName ?? GenerateDefaultFileName()
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string jsonFilePath = saveDialog.FileName;
                    string jsonDirectory = Path.GetDirectoryName(jsonFilePath);
                    
                    // Export optimization objectives if requested
                    List<SkippedPlanInfo> skippedPlans = null;
                    if (includeObjectives && esapiApp != null)
                    {
                        try
                        {
                            var objectivesService = new OptimizationObjectivesExportService();
                            var result = objectivesService.ExportObjectivesToStagingFolder(
                                esapiApp, collection, jsonDirectory);
                            
                            string stagingPath = result.StagingPath;
                            List<ObjectiveFileMapping> fileMappings = result.FileMappings;
                            skippedPlans = result.SkippedPlans;
                            
                            if (!string.IsNullOrEmpty(stagingPath) && fileMappings != null && fileMappings.Count > 0)
                            {
                                // Store ABSOLUTE path for robustness so batch can always locate objectives
                                // (batch script still tries relative fallbacks if this path isn't available)
                                collection.ObjectivesStagingPath = System.IO.Path.GetFullPath(stagingPath);
                                collection.ObjectiveFiles = fileMappings;
                                collection.HasObjectives = true;
                                
                                System.Diagnostics.Debug.WriteLine($"Exported {fileMappings.Count} objective files to {stagingPath}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("No objectives were exported (plans may not have optimization data)");
                            }
                        }
                        catch (Exception objEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error exporting objectives: {objEx.Message}");
                            // Continue with export even if objectives fail
                            ThemedMessageBox.Show($"Warning: Failed to export optimization objectives:\n{objEx.Message}\n\nPatient/plan list will still be exported.",
                                            "Objectives Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    
                    // Export the JSON handoff file
                    var json = collection.ToJson(formatted: true);
                    File.WriteAllText(jsonFilePath, json);

                    // Build success message
                    string message = $"Successfully exported {collection.TotalPatients} patients and {collection.TotalPlans} plans to:\n{jsonFilePath}";
                    
                    if (collection.HasObjectives && collection.ObjectiveFiles != null && collection.ObjectiveFiles.Count > 0)
                    {
                        message += $"\n\n✓ Exported {collection.ObjectiveFiles.Count} optimization objective file(s) to:\n{Path.Combine(jsonDirectory, "Objectives")}";
                        message += "\n\nThese will be automatically copied to patient folders when you run the DicomTools batch file.";
                    }
                    
                    // Show notification about plans where optimization objectives couldn't be exported
                    if (includeObjectives && skippedPlans != null && skippedPlans.Count > 0)
                    {
                        message += $"\n\n⚠ Note: Optimization objectives could NOT be exported for {skippedPlans.Count} plan(s):";
                        
                        // Limit display to first 10 skipped plans to avoid overwhelming message box
                        int displayLimit = Math.Min(skippedPlans.Count, 10);
                        for (int i = 0; i < displayLimit; i++)
                        {
                            var skipped = skippedPlans[i];
                            message += $"\n  • {skipped.PatientId} / {skipped.PlanId}";
                        }
                        
                        if (skippedPlans.Count > displayLimit)
                        {
                            message += $"\n  ... and {skippedPlans.Count - displayLimit} more";
                        }
                        
                        message += "\n\n(Plans were exported, but these plans have no optimization objectives - may be 3D-CRT or 2D plans)";
                    }
                    
                    ThemedMessageBox.Show(message, "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error exporting to file:\n{ex.Message}",
                                "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Imports patient/plan collection from JSON file with user file dialog
        /// </summary>
        public PatientPlanCollection ImportFromFile()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Title = "Import Patient/Plan List",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var collection = PatientPlanCollection.FromJson(json);

                    ThemedMessageBox.Show($"Successfully imported {collection.TotalPatients} patients and {collection.TotalPlans} plans from:\n{openDialog.FileName}",
                                    "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    return collection;
                }

                return null;
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error importing from file:\n{ex.Message}",
                                "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Exports collection to a specific file path (for programmatic use)
        /// </summary>
        public void ExportToPath(PatientPlanCollection collection, string filePath)
        {
            try
            {
                var json = collection.ToJson(formatted: true);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error writing to file {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Imports collection from a specific file path (for programmatic use)
        /// </summary>
        public PatientPlanCollection ImportFromPath(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var json = File.ReadAllText(filePath);
                return PatientPlanCollection.FromJson(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error reading from file {filePath}: {ex.Message}", ex);
            }
        }

        private string GenerateDefaultFileName()
        {
            return $"PatientPlanList_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        }
    }
}
