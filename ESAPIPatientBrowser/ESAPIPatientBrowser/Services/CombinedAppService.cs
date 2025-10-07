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
        public async Task<bool> LaunchWithPatientListAsync(PatientPlanCollection collection)
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

        public bool OpenIndexHtml(PatientPlanCollection handoffCollection = null)
        {
            try
            {
                // Try to find index.html in common relative locations
                var candidates = new System.Collections.Generic.List<string>();

                // 1) Relative to detected CombinedApp executable folder
                var appPath = GetCombinedAppPath();
                var appBase = Path.GetDirectoryName(appPath);
                if (!string.IsNullOrEmpty(appBase))
                {
                    candidates.Add(Path.Combine(appBase, "ui", "public", "index.html"));
                    candidates.Add(Path.Combine(appBase, "..", "ui", "public", "index.html"));
                }

                // 2) Relative to the ESAPIPatientBrowser exe (bin folder)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Walk up several ancestors and try CombinedApp\ui\public\index.html
                for (int i = 0; i <= 7; i++)
                {
                    var ancestor = baseDir;
                    for (int j = 0; j < i; j++) ancestor = Path.GetFullPath(Path.Combine(ancestor, ".."));
                    candidates.Add(Path.Combine(ancestor, "CombinedApp", "ui", "public", "index.html"));
                    candidates.Add(Path.Combine(ancestor, "CombinedApp", "CombinedApp", "ui", "public", "index.html"));
                    candidates.Add(Path.Combine(ancestor, "ui", "public", "index.html"));
                }

                foreach (var file in candidates)
                {
                    if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                    {
                        // If a handoff collection was provided, write it next to index.html so the page can load it
                        if (handoffCollection != null)
                        {
                            try
                            {
                                var indexDir = Path.GetDirectoryName(file);
                                if (!string.IsNullOrEmpty(indexDir))
                                {
                                    var jsonPath = Path.Combine(indexDir, "handoff_patients.json");
                                    var idsPath = Path.Combine(indexDir, "handoff_patient_ids.txt");
                                    var idsJsPath = Path.Combine(indexDir, "handoff_patient_ids.js");
                                    var handoffJsPath = Path.Combine(indexDir, "handoff_patients.js");
                                    // Overwrite each time
                                    _jsonExportService.ExportToPath(handoffCollection, jsonPath);
                                    File.WriteAllText(idsPath, handoffCollection.PatientIdString ?? string.Empty, Encoding.UTF8);
                                    // Also provide a JS bootstrap for file:// contexts
                                    var totalPatients = handoffCollection.TotalPatients;
                                    var totalPlans = handoffCollection.TotalPlans;
                                    var jsContent = "(function(){ try { window.PATIENT_IDS = '" + (handoffCollection.PatientIdString ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'") + "'; " +
                                                    "window.HANDOFF_DATA = { totalPatients: " + totalPatients + ", totalPlans: " + totalPlans + " }; } catch(e){} })();";
                                    File.WriteAllText(idsJsPath, jsContent, Encoding.UTF8);
                                    // Optionally write full handoff JSON JS; ignore failures
                                    try { var compactJson = handoffCollection.ToJson(formatted: false); var handoffJs = "(function(){ try { window.HANDOFF_FULL = " + compactJson + "; } catch(e){} })();"; File.WriteAllText(handoffJsPath, handoffJs, Encoding.UTF8); } catch {}
                                }
                            }
                            catch (Exception wex)
                            {
                                Debug.WriteLine("Failed writing handoff files: " + wex.Message);
                                // Non-fatal; proceed to open UI
                            }
                        }

                        var indexFullPath = Path.GetFullPath(file);
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
                }

                // 3) Prompt user to locate index.html manually
                var dlg = new OpenFileDialog
                {
                    Title = "Locate CombinedApp index.html",
                    Filter = "HTML files (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    var psi2 = new ProcessStartInfo
                    {
                        FileName = dlg.FileName,
                        UseShellExecute = true
                    };
                    Process.Start(psi2);
                    return true;
                }

                ThemedMessageBox.Show("Could not locate CombinedApp index.html. Please verify its location under CombinedApp\\ui\\public.",
                                "Open UI Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Error opening CombinedApp UI:\n{ex.Message}",
                                "Open UI Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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
