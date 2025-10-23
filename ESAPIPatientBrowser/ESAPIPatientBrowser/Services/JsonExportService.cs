using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ESAPIPatientBrowser.Models;
using ESAPIPatientBrowser.Views;

namespace ESAPIPatientBrowser.Services
{
    public class JsonExportService
    {
        /// <summary>
        /// Exports patient/plan collection to JSON file with user file dialog
        /// </summary>
        public bool ExportToFile(PatientPlanCollection collection, string defaultFileName = null)
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
                    var json = collection.ToJson(formatted: true);
                    File.WriteAllText(saveDialog.FileName, json);

                    ThemedMessageBox.Show($"Successfully exported {collection.TotalPatients} patients and {collection.TotalPlans} plans to:\n{saveDialog.FileName}",
                                    "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
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
