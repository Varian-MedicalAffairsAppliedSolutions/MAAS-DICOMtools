using DicomTools.DataModel.CollectFiles;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using DicomTools.DataModel.ReferenceTree;
using DicomTools.DataModel;
using System.CommandLine.IO;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.IO;
using FellowOakDicom.IO.Writer;
using System.Linq;

namespace DicomTools.Retrieve
{
    public class StoreService
    {
        public StoreService(ILogger<DicomStoreHostedService> logger, IConsole console, RetrieveOptions globalRetrieveOptions, DicomAnonymizer dicomAnonymizer)
        {
            m_logger = logger;
            m_console = console;
            m_globalRetrieveOptions = globalRetrieveOptions;
            m_dicomAnonymizer = dicomAnonymizer;

            m_tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            m_collectedPatientSeries = new CollectedPatientSeries();
            m_machineMapping = new Dictionary<string, string>();
            m_defaultMachinesByModel = new Dictionary<string, string>();

            m_anonymizationMapEntries = new List<AnonymizationMapEntry>();
        }

        public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            if (string.IsNullOrEmpty(m_globalRetrieveOptions.Path))
            {
                // TODO: Switch to correct exception
                throw new InvalidOperationException();
            }

            StringBuilder? logMessageBuilder = m_logger.IsEnabled(LogLevel.Information) ? new StringBuilder() : null;

            EnsureFolderExists(m_tempFolder);

            var dataset = request.Dataset;

            // We don't need machine mapping when retrieving plans, or do we?
            var instance = Instance.CreateFromDataset(dataset, m_machineMapping, m_defaultMachinesByModel);

            logMessageBuilder?.AppendLine($"Received {instance.Modality}, Series {instance.SeriesUid.UID}, Instance {instance.InstanceUid.UID}");

            var fileName = instance.GenerateFileName();
            fileName = Path.Combine(m_tempFolder, fileName);

            // Capture original identifiers prior to anonymization
            var originalPatientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
            var originalPatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);

            if (m_globalRetrieveOptions.Anonymize)
            {
                m_dicomAnonymizer.AnonymizeInPlace(dataset, m_globalRetrieveOptions.NewPatientId, m_globalRetrieveOptions.NewPatientName);

                // After anonymization, record mapping for audit
                var anonymizedPatientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                var anonymizedPatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);

                lock (m_anonymizationMapEntries)
                {
                    if (!m_anonymizationMapEntries.Any(e => e.OriginalPatientId == originalPatientId && e.AnonymizedPatientId == anonymizedPatientId))
                    {
                        m_anonymizationMapEntries.Add(new AnonymizationMapEntry
                        {
                            OriginalPatientId = originalPatientId,
                            OriginalPatientName = originalPatientName,
                            AnonymizedPatientId = anonymizedPatientId,
                            AnonymizedPatientName = anonymizedPatientName,
                            TimestampUtc = DateTime.UtcNow
                        });
                    }
                }
            }

            using var unvalidatedScope = new UnvalidatedScope(dataset);  // To prevent validation errors like leading zero in UUID.
            var saveDicomFile = new DicomFile(dataset);
            await saveDicomFile.SaveAsync(fileName);

            var anonymizedInstance = Instance.CreateFromDataset(dataset, m_machineMapping, m_defaultMachinesByModel);
            if (m_globalRetrieveOptions.Anonymize && logMessageBuilder != null)
                logMessageBuilder.AppendLine($"Anonymized {anonymizedInstance.Modality}, Series {anonymizedInstance.SeriesUid.UID}, Instance {anonymizedInstance.InstanceUid.UID}");

            if (logMessageBuilder != null)
                m_logger.LogInformation(logMessageBuilder.ToString());

            m_collectedPatientSeries.Add(anonymizedInstance.PatientId, anonymizedInstance, fileName);
            m_console.Out.WriteLine($"Retrieved {Path.GetFileName(fileName)}");

            return await Task.FromResult(new DicomCStoreResponse(request, DicomStatus.Success));
        }

        public void ProcessCollectedSeries()
        {
            if (string.IsNullOrEmpty(m_globalRetrieveOptions.Path))
                return; // We are not retrieving.

            if (m_collectedPatientSeries.Count == 0)
                return; // Did not find anything.

            var targetPath = m_globalRetrieveOptions.Path;
            EnsureFolderExists(targetPath);

            foreach (var patientSeries in m_collectedPatientSeries)
            {
                var referenceTree = ReferenceTree.Create(m_logger, m_console, patientSeries.CollectedSeries);

                foreach (var plan in referenceTree.Plans)
                {
                    if (!string.IsNullOrEmpty(m_globalRetrieveOptions.PlanId) && plan.Instance.Label != m_globalRetrieveOptions.PlanId)
                        continue;

                    if (m_globalRetrieveOptions.OnlyApprovedPlans && plan.Instance.ApprovalStatus != ApprovalStatus.Approved)
                        continue;

                    MoveFile(plan, targetPath);
                    if (plan.StructureSet != null)
                    {
                        MoveFile(plan.StructureSet, targetPath);
                        if (plan.StructureSet.ImageSeries != null)
                            MoveSeries(plan.StructureSet.ImageSeries, targetPath);

                        foreach (var dose in plan.Doses)
                            MoveFile(dose, targetPath);

                        foreach (var beam in plan.Beams)
                        {
                            foreach (var drrImage in beam.DrrImageTreeItems)
                                MoveFile(drrImage, targetPath);
                            foreach (var rtImage in beam.RtImageTreeItems)
                                MoveFile(rtImage, targetPath);
                            foreach (var treatmentRecord in beam.TreatmentRecordTreeItems)
                                MoveFile(treatmentRecord, targetPath);
                        }

                        MoveSeries(plan.ConeBeamImageSeries, targetPath);

                        foreach (var registration in plan.Registrations)
                        {
                            MoveFile(registration, targetPath);
                            foreach (var structureSet in registration.StructureSets)
                            {
                                MoveFile(structureSet, targetPath);
                                if (structureSet.ImageSeries != null)
                                    MoveSeries(structureSet.ImageSeries, targetPath);
                            }
                            MoveSeries(registration.ImageSeries, targetPath);
                        }
                    }
                }

                if (string.IsNullOrEmpty(m_globalRetrieveOptions.PlanId))
                {
                    foreach (var structureSet in referenceTree.StructureSets)
                    {
                        MoveFile(structureSet, targetPath);
                        if (structureSet.ImageSeries != null)
                            MoveSeries(structureSet.ImageSeries, targetPath);
                    }
                    foreach (var dose in referenceTree.Doses)
                        MoveFile(dose, targetPath);

                    MoveSeries(referenceTree.RtImages, targetPath);
                    MoveSeries(referenceTree.ConeBeamImages, targetPath);
                    MoveSeries(referenceTree.CtImages, targetPath);
                    MoveSeries(referenceTree.MrImages, targetPath);
                    MoveSeries(referenceTree.PetImages, targetPath);

                    foreach (var registration in referenceTree.Registrations)
                        MoveFile(registration, targetPath);
                }

                if (m_globalRetrieveOptions.ShowTree)
                {
                    m_console.Out.WriteLine();
                    m_console.Out.WriteLine($"Patient: {patientSeries.PatientId}");
                    m_console.Out.WriteLine(referenceTree.ToString());
                }
            }

            EnsureFolderIsDeleted(m_tempFolder);

            // Write anonymization mapping CSV if anonymization was used
            if (m_globalRetrieveOptions.Anonymize && m_anonymizationMapEntries.Count > 0)
            {
                try
                {
                    var csvPath = Path.Combine(m_globalRetrieveOptions.Path, "anonymization_map.csv");
                    var sb = new StringBuilder();
                    sb.AppendLine("OriginalPatientId,OriginalPatientName,AnonymizedPatientId,AnonymizedPatientName,TimestampUtc");
                    foreach (var entry in m_anonymizationMapEntries)
                    {
                        string Quote(string s) => string.IsNullOrEmpty(s) ? string.Empty : $"\"{s.Replace("\"", "\"\"")}\"";
                        sb.AppendLine(string.Join(",", new[]
                        {
                            Quote(entry.OriginalPatientId),
                            Quote(entry.OriginalPatientName),
                            Quote(entry.AnonymizedPatientId),
                            Quote(entry.AnonymizedPatientName),
                            entry.TimestampUtc.ToString("o")
                        }));
                    }
                    File.WriteAllText(csvPath, sb.ToString());
                    m_console.Out.WriteLine($"Anonymization mapping saved: {csvPath}");
                }
                catch (Exception ex)
                {
                    m_logger.LogError($"Failed to write anonymization mapping CSV: {ex.Message}");
                }
            }
        }

        private void MoveFile(TreeItem treeItem, string targetPath)
        {
            if (!File.Exists(treeItem.FileName))
                return; // Already moved

            var targetFileName = Path.Combine(targetPath, Path.GetFileName(treeItem.FileName));
            if (File.Exists(targetFileName))
            {
                m_console.Out.WriteLine($"File {targetFileName} already exists, deleting it.");
                File.Delete(targetFileName);
            }

            m_logger.LogInformation(targetFileName);
            File.Move(treeItem.FileName, targetFileName);
        }

        private void MoveSeries<T>(IReadOnlyList<SeriesTreeItem<T>> seriesList, string targetPath) where T : Image
        {
            foreach (var imageSeries in seriesList)
                MoveSeries(imageSeries, targetPath);
        }

        private void MoveSeries<T>(SeriesTreeItem<T> series, string targetPath) where T : Image
        {
            foreach (var imageInstance in series.Instances)
                MoveFile(imageInstance, targetPath);
        }

        internal List<DataModel.RtPlan> FilterAndGetMatchingPlans()
        {
            var matchingPlans = new List<DataModel.RtPlan>();

            foreach (var patientSeries in m_collectedPatientSeries)
            {
                var referenceTree = ReferenceTree.Create(m_logger, m_console, patientSeries.CollectedSeries);

                foreach (var plan in referenceTree.Plans)
                {
                    // Apply filters
                    if (!string.IsNullOrEmpty(m_globalRetrieveOptions.PlanId) && 
                        plan.Instance.Label != m_globalRetrieveOptions.PlanId)
                        continue;

                    if (m_globalRetrieveOptions.OnlyApprovedPlans && 
                        plan.Instance.ApprovalStatus != ApprovalStatus.Approved)
                        continue;

                    // This plan matches - add it
                    matchingPlans.Add(plan.Instance);
                }
            }

            return matchingPlans;
        }

        private static void EnsureFolderExists(string folderName)
        {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
        }

        private static void EnsureFolderIsDeleted(string folderName)
        {
            if (Directory.Exists(folderName))
                Directory.Delete(folderName, recursive: true);
        }


        private readonly RetrieveOptions m_globalRetrieveOptions;

        private readonly string m_tempFolder;

        private readonly ILogger m_logger;

        private readonly IConsole m_console;

        private readonly DicomAnonymizer m_dicomAnonymizer;

        private readonly CollectedPatientSeries m_collectedPatientSeries;

        private readonly Dictionary<string, string> m_machineMapping;

        private readonly Dictionary<string, string> m_defaultMachinesByModel;

        private readonly List<AnonymizationMapEntry> m_anonymizationMapEntries;

        private sealed class AnonymizationMapEntry
        {
            public string OriginalPatientId { get; set; } = string.Empty;
            public string OriginalPatientName { get; set; } = string.Empty;
            public string AnonymizedPatientId { get; set; } = string.Empty;
            public string AnonymizedPatientName { get; set; } = string.Empty;
            public DateTime TimestampUtc { get; set; }
        }
    }
}
