using System;
using System.IO;
using Newtonsoft.Json;
using ESAPIPatientBrowser.Models;

namespace ESAPIPatientBrowser.Services
{
    /// <summary>
    /// Service for persisting user settings across application sessions
    /// </summary>
    public static class SettingsService
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ESAPIPatientBrowser"
        );

        private static readonly string AdvancedSearchSettingsFile = Path.Combine(
            SettingsFolder,
            "AdvancedSearchSettings.json"
        );

        /// <summary>
        /// Saves Advanced Search criteria to disk
        /// </summary>
        public static void SaveAdvancedSearchCriteria(AdvancedSearchCriteria criteria)
        {
            try
            {
                // Ensure settings directory exists
                if (!Directory.Exists(SettingsFolder))
                {
                    Directory.CreateDirectory(SettingsFolder);
                }

                // Serialize and save
                var json = JsonConvert.SerializeObject(criteria, Formatting.Indented);
                File.WriteAllText(AdvancedSearchSettingsFile, json);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - settings save should not crash the app
                System.Diagnostics.Debug.WriteLine($"Failed to save Advanced Search settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads saved Advanced Search criteria from disk
        /// </summary>
        /// <returns>Saved criteria, or a new empty criteria if none exists</returns>
        public static AdvancedSearchCriteria LoadAdvancedSearchCriteria()
        {
            try
            {
                if (File.Exists(AdvancedSearchSettingsFile))
                {
                    var json = File.ReadAllText(AdvancedSearchSettingsFile);
                    var criteria = JsonConvert.DeserializeObject<AdvancedSearchCriteria>(json);
                    return criteria ?? new AdvancedSearchCriteria();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - return empty criteria instead
                System.Diagnostics.Debug.WriteLine($"Failed to load Advanced Search settings: {ex.Message}");
            }

            return new AdvancedSearchCriteria();
        }

        /// <summary>
        /// Clears saved Advanced Search criteria
        /// </summary>
        public static void ClearAdvancedSearchCriteria()
        {
            try
            {
                if (File.Exists(AdvancedSearchSettingsFile))
                {
                    File.Delete(AdvancedSearchSettingsFile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear Advanced Search settings: {ex.Message}");
            }
        }
    }
}

