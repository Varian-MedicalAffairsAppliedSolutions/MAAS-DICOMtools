using FellowOakDicom;
using System.CommandLine;
using System.CommandLine.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DicomTools.DataModel.CollectFiles
{
    internal static class FileCollector
    {
        internal static CollectedPatientSeries CollectFiles(
            ILogger logger, IConsole console,
            string path, string searchPattern,
            IReadOnlyDictionary<string, string> machineMapping, IReadOnlyDictionary<string, string> defaultMachinesByModel)
        {
            var collectedPatientSeries = new CollectedPatientSeries();
            CollectFiles(logger, console, collectedPatientSeries, path, searchPattern, machineMapping, defaultMachinesByModel);
            return collectedPatientSeries;
        }

        private static void CollectFiles(ILogger logger, IConsole console, CollectedPatientSeries collectedPatientSeries,
            string path, string searchPattern,
            IReadOnlyDictionary<string, string> machineMapping, IReadOnlyDictionary<string, string> defaultMachinesByModel)
        {
            console.Out.WriteLine($"Scanning {path}...");
            var fileNames = Directory.EnumerateFiles(path, searchPattern).ToList();
            console.Out.WriteLine($"Found {fileNames.Count} files.");
            foreach (var fileName in fileNames)
            {
                try
                {
                    var dicomFile = DicomFile.Open(fileName, FileReadOption.SkipLargeTags);

                    var instance = Instance.CreateFromDataset(dicomFile.Dataset, machineMapping, defaultMachinesByModel);

                    if (collectedPatientSeries.TryGetValue(instance.PatientId, out var collectedSeries))
                    {
                        // Patient found.
                        if (collectedSeries!.TryGetValue(instance.SeriesUid, out var collectedInstances))
                        {
                            // Series found.
                            if (collectedInstances!.TryGetValue(instance.InstanceUid, out var instanceAndFileName))
                            {
                                // Instance found.
                                var stringBuilder = new StringBuilder();
                                stringBuilder.AppendLine($"{instance.Modality}: {instance.InstanceUid.UID} already in dictionary.");
                                stringBuilder.AppendLine($"Files: {fileName}");
                                stringBuilder.AppendLine($"and {instanceAndFileName!.FileName}");
                                logger.LogWarning(stringBuilder.ToString());
                            }
                            else
                            {
                                // New Instance.
                                collectedInstances.Add(instance, fileName);
                            }
                        }
                        else
                        {
                            // New Series (and Instance).
                            collectedSeries.Add(instance, fileName);
                        }
                    }
                    else
                    {
                        // New patient (and Series and Instance).
                        collectedPatientSeries.Add(instance.PatientId, instance, fileName);
                    }
                }
                catch (DicomFileException ex)
                {
                    logger.LogError($"File {fileName} is not a dicom file. ({ex.Message})");
                }
            }

            var directories = Directory.EnumerateDirectories(path);
            foreach (var directory in directories)
            {
                CollectFiles(logger, console, collectedPatientSeries, directory, searchPattern, machineMapping, defaultMachinesByModel);
            }

            foreach (var patientSeries in collectedPatientSeries)
            {
                console.Out.Write($"Patient {patientSeries.PatientId}: ");
                console.Out.WriteLine($"{patientSeries.CollectedSeries.Count} series.");
            }
        }
    }
}
