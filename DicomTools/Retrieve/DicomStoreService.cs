using DicomTools.DataModel;
using DicomTools.DataModel.ReferenceTree;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.IO;
using DicomTools.Configuration;
using DicomTools.DataModel.CollectFiles;
using Microsoft.Extensions.Configuration;

namespace DicomTools.Retrieve
{
    public class DicomStoreService : IHostedService
    {
        public DicomStoreService(IConfiguration configuration, ILogger<DicomStoreService> logger, IConsole console, IHostApplicationLifetime applicationLifetime,
            DicomAnonymizer dicomAnonymizer, DicomStorageRetrieveOptions retrieveOptions)
        {
            m_logger = logger;
            m_console = console;
            m_applicationLifetime = applicationLifetime;
            m_dicomAnonymizer = dicomAnonymizer;

            m_configuration = configuration.GetRequiredSection("DicomStorage").Get<DicomStorageConfiguration>()!;
            m_retrieveOptions = retrieveOptions;

            m_tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            m_collectedPatientSeries = new CollectedPatientSeries();
            m_machineMapping = new Dictionary<string, string>();
            m_defaultMachinesByModel = new Dictionary<string, string>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var portNumber = m_configuration.PortNumber;
            m_dicomServer = DicomServerFactory.Create<DicomStore>(portNumber, logger: m_logger, userState: this);

            m_applicationLifetime.ApplicationStopping.Register(ProcessCollectedSeries);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            m_dicomServer?.Stop();
            m_dicomServer?.Dispose();
            m_dicomServer = null;

            EnsureFolderIsDeleted(m_tempFolder);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Do the export here as we want to anonymize everything in same "session".
        /// </summary>
        public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            if (string.IsNullOrEmpty(m_retrieveOptions.Path))
            {
                // TODO: Switch to correct exception
                throw new InvalidOperationException();
            }

            EnsureFolderExists(m_tempFolder);

            var dataset = request.Dataset;

            // We don't need machine mapping when retrieving plans, or do we?
            var instance = Instance.CreateFromDataset(dataset, m_machineMapping, m_defaultMachinesByModel);

            m_logger.LogInformation($"Received {instance.Modality}");

            var fileName = instance.GenerateFileName();
            fileName = Path.Combine(m_tempFolder, fileName);

            if (m_retrieveOptions.Anonymize)
                m_dicomAnonymizer.AnonymizeInPlace(dataset, m_retrieveOptions.NewPatientId, m_retrieveOptions.NewPatientName);

            var saveDicomFile = new DicomFile(dataset);
            await saveDicomFile.SaveAsync(fileName);

            var anonymizedInstance = Instance.CreateFromDataset(dataset, m_machineMapping, m_defaultMachinesByModel);
            m_collectedPatientSeries.Add(anonymizedInstance.PatientId, anonymizedInstance, fileName);

            return await Task.FromResult(new DicomCStoreResponse(request, DicomStatus.Success));
        }

        public void ProcessCollectedSeries()
        {
            if (string.IsNullOrEmpty(m_retrieveOptions.Path))
                return; // We are not retrieving.

            if (m_collectedPatientSeries.Count == 0)
                return; // Did not find anything.

            var targetPath = m_retrieveOptions.Path;
            EnsureFolderExists(targetPath);

            foreach (var patientSeries in m_collectedPatientSeries)
            {
                var referenceTree = ReferenceTree.Create(m_logger, m_console, patientSeries.CollectedSeries);

                foreach (var plan in referenceTree.Plans)
                {
                    if (!string.IsNullOrEmpty(m_retrieveOptions.PlanId) && plan.Instance.Label != m_retrieveOptions.PlanId)
                        continue;

                    if (m_retrieveOptions.OnlyApprovedPlans && plan.Instance.ApprovalStatus != ApprovalStatus.Approved)
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

                if (string.IsNullOrEmpty(m_retrieveOptions.PlanId))
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

                if (m_retrieveOptions.ShowTree)
                {
                    m_console.Out.WriteLine();
                    m_console.Out.WriteLine($"Patient: {patientSeries.PatientId}");
                    m_console.Out.WriteLine(referenceTree.ToString());
                }
            }
        }

        private void MoveFile(TreeItem treeItem, string targetPath)
        {
            var targetFileName = Path.Combine(targetPath, Path.GetFileName(treeItem.FileName));
            if (File.Exists(targetFileName))
            {
                m_console.Out.WriteLine($"File {targetFileName} already exists, deleting it.");
                File.Delete(targetFileName);
            }

            File.Move(treeItem.FileName, targetFileName);
        }

        private void MoveSeries<T>(IReadOnlyList<SeriesTreeItem<T>> seriesList, string targetPath) where T: Image
        {
            foreach (var imageSeries in seriesList)
                MoveSeries(imageSeries, targetPath);
        }

        private void MoveSeries<T>(SeriesTreeItem<T> series, string targetPath) where T : Image
        {
            foreach (var imageInstance in series.Instances)
                MoveFile(imageInstance, targetPath);
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

        private readonly DicomStorageConfiguration m_configuration;

        private readonly DicomStorageRetrieveOptions m_retrieveOptions;

        private readonly string m_tempFolder;

        private readonly ILogger m_logger;

        private readonly IConsole m_console;

        private readonly IHostApplicationLifetime m_applicationLifetime;

        private IDicomServer? m_dicomServer;

        private readonly DicomAnonymizer m_dicomAnonymizer;

        private readonly CollectedPatientSeries m_collectedPatientSeries;

        private readonly Dictionary<string, string> m_machineMapping;

        private readonly Dictionary<string, string> m_defaultMachinesByModel;
    }
}
