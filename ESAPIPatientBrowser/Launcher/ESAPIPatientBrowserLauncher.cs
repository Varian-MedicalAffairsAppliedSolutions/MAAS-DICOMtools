using System.Windows;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using System.Diagnostics;
using System.IO;
using System;

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            try
            {
                // Prepare and launch application
                string launcherPath = Path.GetDirectoryName(GetSourceFilePath());
                string esapiStandaloneExecutable = @"ESAPIPatientBrowser.exe";
                
                // Validate the launcher path was found
                if (string.IsNullOrEmpty(launcherPath))
                {
                    MessageBox.Show("Error: Unable to determine the launcher script location.",
                                    "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Construct arguments for the executable (optional patient ID if available)
                string arguments = "";
                if (context.Patient != null)
                {
                    arguments = string.Format("\"{0}\"", context.Patient.Id);
                }

                // Validate the executable path
                string executablePath = Path.Combine(launcherPath, esapiStandaloneExecutable);
                if (!File.Exists(executablePath))
                {
                    MessageBox.Show(string.Format("Error: The executable '{0}' was not found at '{1}'.", 
                                    esapiStandaloneExecutable, launcherPath),
                                    "Executable Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create the process start info
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = launcherPath
                };

                // Start the process
                Process process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new ApplicationException("Failed to start the Patient List Builder process.");
                }
                
                // The process will run independently - no success message needed
            }
            catch (ApplicationException appEx)
            {
                MessageBox.Show(string.Format("Application Error: {0}", appEx.Message), 
                                "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unexpected Error: {0}\n\nStack Trace:\n{1}", ex.Message, ex.StackTrace),
                                "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath;
        }
    }
}
