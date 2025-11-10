using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ESAPIPatientBrowser.Models;
using ESAPIPatientBrowser.Views;
using Microsoft.Win32;
using ESAPIApp = VMS.TPS.Common.Model.API.Application;

namespace ESAPIPatientBrowser.Services
{
    public class CombinedAppService
    {
        private readonly JsonExportService _jsonExportService;
        private readonly string _combinedAppPath;
        private readonly string _combinedAppUrl;

        public CombinedAppService(JsonExportService jsonExportService)
        {
            _jsonExportService = jsonExportService ?? throw new ArgumentNullException(nameof(jsonExportService));
            _combinedAppPath = ConfigurationManager.AppSettings["CombinedAppPath"] ?? @"..\CombinedApp\CombinedApp.exe";
            _combinedAppUrl = ConfigurationManager.AppSettings["CombinedAppUrl"] ?? "http://localhost:8080";
        }

        /// <summary>
        /// Launches CombinedApp and sends the patient/plan collection
        /// </summary>
        public async Task<bool> LaunchWithPatientListAsync(PatientPlanCollection collection, bool includeObjectives = false, ESAPIApp esapiApp = null)
        {
            try
            {
                // First, try to launch CombinedApp
                if (!await LaunchCombinedAppAsync())
                {
                    return false;
                }

                // Wait a moment for CombinedApp to start up
                await Task.Delay(2000);

                // Try to send the patient list via HTTP API (if implemented)
                // If that fails, fall back to file-based approach
                if (!await SendPatientListViaApiAsync(collection))
                {
                    return await SendPatientListViaFileAsync(collection);
                }

                return true;
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error launching CombinedApp with patient list:\n{ex.Message}",
                                "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Launches CombinedApp standalone
        /// </summary>
        public async Task<bool> LaunchCombinedAppAsync()
        {
            try
            {
                // Check if CombinedApp is already running
                if (await IsCombinedAppRunningAsync())
                {
                    ThemedMessageBox.Show("CombinedApp is already running. The patient list will be sent to the existing instance.",
                                    "CombinedApp Running", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }

                // Get the full path to CombinedApp
                var appPath = GetCombinedAppPath();
                if (!File.Exists(appPath))
                {
                    ThemedMessageBox.Show($"CombinedApp not found at:\n{appPath}\n\nPlease check the configuration.",
                                    "Application Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Launch CombinedApp
                var startInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = Path.GetDirectoryName(appPath)
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        ThemedMessageBox.Show("Failed to start CombinedApp.",
                                        "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error launching CombinedApp:\n{ex.Message}",
                                "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool OpenIndexHtml(PatientPlanCollection handoffCollection = null, bool includeObjectives = false, ESAPIApp esapiApp = null)
        {
            try
            {
                // Try to find index.html in common relative locations
                var candidates = new System.Collections.Generic.List<string>();

                // 1) Relative to detected DicomTools executable folder
                var appPath = GetCombinedAppPath();
                var appBase = Path.GetDirectoryName(appPath);
                if (!string.IsNullOrEmpty(appBase))
                {
                    candidates.Add(Path.Combine(appBase, "UI", "public", "index.html"));
                    candidates.Add(Path.Combine(appBase, "DicomTools", "UI", "public", "index.html"));
                }

                // 2) Relative to the ESAPIPatientBrowser exe (bin folder)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Walk up several ancestors and try various paths
                for (int i = 0; i <= 7; i++)
                {
                    var ancestor = baseDir;
                    for (int j = 0; j < i; j++) ancestor = Path.GetFullPath(Path.Combine(ancestor, ".."));

                    // New structure: MAAS-DICOMtools\DicomTools\UI\public\index.html
                    candidates.Add(Path.Combine(ancestor, "MAAS-DICOMtools", "DicomTools", "UI", "public", "index.html"));
                    candidates.Add(Path.Combine(ancestor, "DicomTools", "UI", "public", "index.html"));

                    // Legacy structure support
                    candidates.Add(Path.Combine(ancestor, "CombinedApp", "ui", "public", "index.html"));
                    candidates.Add(Path.Combine(ancestor, "ui", "public", "index.html"));
                }

                string foundIndexPath = null;

                foreach (var file in candidates)
                {
                    if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                    {
                        foundIndexPath = file;
                        break;
                    }
                }

                // 3) If not found automatically, prompt user to locate index.html manually
                if (foundIndexPath == null)
                {
                    var dlg = new OpenFileDialog
                    {
                        Title = "Locate DICOMTools index.html",
                        Filter = "HTML files (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*",
                        FileName = "index.html"
                    };
                    if (dlg.ShowDialog() == true)
                    {
                        foundIndexPath = dlg.FileName;
                    }
                    else
                    {
                        ThemedMessageBox.Show("Could not locate DICOMTools index.html. Please verify its location under MAAS-DICOMtools\\DicomTools\\UI\\public.",
                                        "Open UI Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }

                // Write handoff files if collection was provided
                if (handoffCollection != null && foundIndexPath != null)
                {
                    WriteHandoffFiles(foundIndexPath, handoffCollection, includeObjectives, esapiApp);
                }

                // Open the index.html file in default browser
                var indexFullPath = Path.GetFullPath(foundIndexPath);
                string fileUrl = new Uri(indexFullPath).AbsoluteUri;

                // Append patientIds query parameter if provided
                if (handoffCollection != null)
                {
                    var ids = handoffCollection.PatientIdString ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(ids))
                    {
                        fileUrl += (fileUrl.Contains("?") ? "&" : "?") +
                                   "patientIds=" + Uri.EscapeDataString(ids);
                    }
                }

                var psi = new ProcessStartInfo
                {
                    FileName = fileUrl,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error opening DICOMTools UI:\n{ex.Message}",
                                "Open UI Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void WriteHandoffFiles(string indexHtmlPath, PatientPlanCollection handoffCollection, bool includeObjectives, ESAPIApp esapiApp)
        {
            try
            {
                var indexDir = Path.GetDirectoryName(indexHtmlPath);
                if (string.IsNullOrEmpty(indexDir)) return;

                // Optionally export optimization objectives into an Objectives folder next to the UI
                if (includeObjectives && esapiApp != null)
                {
                    try
                    {
                        var objectivesService = new OptimizationObjectivesExportService();
                        var result = objectivesService.ExportObjectivesToStagingFolder(esapiApp, handoffCollection, indexDir);

                        var stagingPath = result.StagingPath;
                        var fileMappings = result.FileMappings;
                        if (!string.IsNullOrEmpty(stagingPath) && fileMappings != null && fileMappings.Count > 0)
                        {
                            handoffCollection.ObjectivesStagingPath = Path.GetFullPath(stagingPath);
                            handoffCollection.ObjectiveFiles = fileMappings;
                            handoffCollection.HasObjectives = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Objectives export during handoff failed: " + ex.Message);
                        // Non-fatal, continue without objectives
                    }
                }

                var jsonPath = Path.Combine(indexDir, "handoff_patients.json");
                var handoffJsPath = Path.Combine(indexDir, "handoff_patients.js");

                // Write JSON file (for manual import via "Import JSON" button)
                _jsonExportService.ExportToPath(handoffCollection, jsonPath);

                // Write full handoff data as JavaScript (for auto-load on page launch)
                try
                {
                    var compactJson = handoffCollection.ToJson(formatted: false);
                    var handoffJs = "(function(){ try { window.HANDOFF_FULL = " + compactJson + "; } catch(e){} })();";
                    File.WriteAllText(handoffJsPath, handoffJs, Encoding.UTF8);
                }
                catch (Exception jsEx)
                {
                    Debug.WriteLine("Failed writing handoff JS: " + jsEx.Message);
                    // Non-fatal if full handoff fails
                }
            }
            catch (Exception wex)
            {
                Debug.WriteLine("Failed writing handoff files: " + wex.Message);
                // Non-fatal; UI will still open
            }
        }

        private async Task<bool> IsCombinedAppRunningAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    var response = await client.GetAsync($"{_combinedAppUrl}/health");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> SendPatientListViaApiAsync(PatientPlanCollection collection)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var json = collection.ToJson();
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{_combinedAppUrl}/api/import-patients", content);

                    if (response.IsSuccessStatusCode)
                    {
                        ThemedMessageBox.Show("Patient list successfully sent to CombinedApp!",
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        return true;
                    }
                    else
                    {
                        // API not available, fall back to file method
                        return false;
                    }
                }
            }
            catch
            {
                // API not available, fall back to file method
                return false;
            }
        }

        private Task<bool> SendPatientListViaFileAsync(PatientPlanCollection collection)
        {
            try
            {
                // Create a temporary file that CombinedApp can pick up
                var tempPath = Path.Combine(Path.GetTempPath(), "ESAPIPatientBrowser");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                var fileName = $"PatientList_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(tempPath, fileName);

                _jsonExportService.ExportToPath(collection, filePath);

                // Show message with instructions
                var message = $"Patient list exported to temporary file:\n{filePath}\n\n" +
                              "In CombinedApp, use the 'Import JSON' button in the Export panel to load this file.";

                ThemedMessageBox.Show(message, "Patient List Ready", MessageBoxButton.OK, MessageBoxImage.Information);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error creating temporary file:\n{ex.Message}",
                                "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return Task.FromResult(false);
            }
        }

        private string GetCombinedAppPath()
        {
            // Try relative path first
            var relativePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _combinedAppPath);
            if (File.Exists(relativePath))
            {
                return relativePath;
            }

            // Try absolute path
            if (Path.IsPathRooted(_combinedAppPath) && File.Exists(_combinedAppPath))
            {
                return _combinedAppPath;
            }

            // Default fallback
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\CombinedApp\CombinedApp.exe");
        }
    }
}

