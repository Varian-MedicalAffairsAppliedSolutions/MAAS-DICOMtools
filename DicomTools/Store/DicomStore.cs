using System.CommandLine;
using System.CommandLine.IO;
using System.Text;
using DicomTools.DataModel;
using DicomTools.DataModel.ReferenceTree;
using FellowOakDicom;
using FellowOakDicom.IO.Writer;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;

namespace DicomTools.Store
{
    internal class DicomStore
    {
        internal DicomStore(ILogger logger, IConsole console, string hostName, int hostPort, string callingAet, string calledAet)
        {
            m_logger = logger;
            m_console = console;

            m_client = DicomClientFactory.Create(hostName, hostPort, useTls: false, callingAe: callingAet, calledAe: calledAet);
            m_client.NegotiateAsyncOps();
        }

        internal async Task SendReferenceTree(ReferenceTree treeItems, string statusFileName)
        {
            HashSet<string>? statusLines = null;
            if (!string.IsNullOrEmpty(statusFileName))
            {
                if (File.Exists(statusFileName))
                    statusLines = new HashSet<string>(await File.ReadAllLinesAsync(statusFileName));
            }

            foreach (var planTreeItem in treeItems.Plans)
            {
                if (planTreeItem.StructureSet == null && planTreeItem.Instance.ReferencedStructureSet != null)
                {
                    m_logger.LogError($"PlanTreeItem {planTreeItem.FileName} referenced StructureSetTreeItem {planTreeItem.Instance.ReferencedStructureSet.InstanceUid.UID} not found.");
                    m_logger.LogError("Still trying to store plan.");
                }
                else
                {
                    if (planTreeItem.StructureSet != null)
                        await SendStructureSet(planTreeItem.StructureSet, statusFileName, statusLines);
                }

                var planFileName = planTreeItem.FileName;
                var planDicomStatus = await SendDatasetIfNotSend(statusFileName, statusLines, planTreeItem.Instance, planFileName);
                if (planDicomStatus == DicomStatus.Cancel)
                    continue;
                if (planDicomStatus != DicomStatus.Success)
                    return;
                planTreeItem.HasBeenSent = true;

                await SendImageSeriesList(planTreeItem.ConeBeamImageSeries, statusFileName, statusLines);

                foreach (var doseTreeItem in planTreeItem.Doses)
                {
                    var dicomStatus = await SendDatasetIfNotSend(statusFileName, statusLines, doseTreeItem.Instance, doseTreeItem.FileName);
                    if (dicomStatus != DicomStatus.Success)
                        return;
                    doseTreeItem.HasBeenSent = true;
                }

                foreach (var registrationTreeItem in planTreeItem.Registrations)
                {
                    await SendImageSeriesList(registrationTreeItem.ImageSeries, statusFileName, statusLines);
                    foreach (var structureSetTreeItem in registrationTreeItem.StructureSets)
                        await SendStructureSet(structureSetTreeItem, statusFileName, statusLines);
                }
            }

            if (treeItems.Doses.Count > 0)
            {
                m_logger.LogWarning("ARIA does not accept doses without plan.");
                m_logger.LogError("Still trying to store plan.");
            }

            foreach (var doseTreeItem in treeItems.Doses)
            {
                // Find plan
                var plan = treeItems.Plans.SingleOrDefault(p =>
                    p.Instance.InstanceUid == doseTreeItem.Instance.ReferencedPlan.InstanceUid);

                if (plan == null)
                {
                    m_logger.LogError(doseTreeItem.FileName);
                    m_logger.LogError($"Referenced plan not found ({doseTreeItem.Instance.ReferencedPlan.InstanceUid.UID}).");
                    continue;
                }

                if (!plan.Instance.UsesVarianTreatmentUnit)
                {
                    m_logger.LogError(doseTreeItem.FileName);
                    m_logger.LogError($"Referenced plan treatment unit is not Varian ({plan.Instance.TreatmentMachineManufacturer}).");
                    continue;
                }

                if (!plan.HasBeenSent)
                {
                    m_logger.LogError(doseTreeItem.FileName);
                    m_logger.LogError($"Referenced plan has not been sent ({plan.Instance.InstanceUid.UID}).");
                    continue;
                }

                var doseFileName = doseTreeItem.FileName;
                var dicomStatus = await SendDatasetIfNotSend(statusFileName, statusLines, doseTreeItem.Instance, doseFileName);
                if (dicomStatus != DicomStatus.Success)
                    return;
                doseTreeItem.HasBeenSent = true;
            }

            // Supporting images
            await SendImageSeriesList(treeItems.CtImages, statusFileName, statusLines);
            await SendImageSeriesList(treeItems.MrImages, statusFileName, statusLines);
            await SendImageSeriesList(treeItems.PetImages, statusFileName, statusLines);
            await SendImageSeriesList(treeItems.RtImages, statusFileName, statusLines);
            await SendImageSeriesList(treeItems.ConeBeamImages, statusFileName, statusLines);

            foreach (var structureSetTreeItem in treeItems.StructureSets)
                await SendStructureSet(structureSetTreeItem, statusFileName, statusLines);
        }

        private async Task SendStructureSet(StructureSetTreeItem structureSet, string statusFileName, HashSet<string>? statusLines)
        {
            if (structureSet.ImageSeries != null)
                await SendImageSeries(structureSet.ImageSeries, statusFileName, statusLines);

            var structureSetFileName = structureSet.FileName;
            var dicomStatus = await SendDatasetIfNotSend(statusFileName, statusLines, structureSet.Instance, structureSetFileName);
            if (dicomStatus != DicomStatus.Success)
                return;
            structureSet.HasBeenSent = true;
        }

        private async Task SendImageSeriesList<T>(IReadOnlyList<SeriesTreeItem<T>> images, string statusFileName, HashSet<string>? statusLines) where T : Image
        {
            foreach (var imageTreeItem in images)
                await SendImageSeries(imageTreeItem, statusFileName, statusLines);
        }

        private async Task SendImageSeries<T>(SeriesTreeItem<T> imageSeries, string statusFileName, HashSet<string>? statusLines) where T : Image
        {
            foreach (var image in imageSeries.Instances)
            {
                var imageFileName = image.FileName;
                var dicomStatus = await SendDatasetIfNotSend(statusFileName, statusLines, image.Instance, imageFileName);
                if (dicomStatus == DicomStatus.Success)
                    image.HasBeenSent = true;
            }
        }

        private async Task<DicomStatus> SendDatasetIfNotSend(string statusFileName, HashSet<string>? statusLines, Instance instance, string fileName)
        {
            if (statusLines != null && statusLines.Contains(fileName))
            {
                m_logger.LogError($"Skipping {fileName} as it is already stored.");
                return DicomStatus.Success;
            }

            DicomStatus dicomStatus;
            switch (instance.Modality.Type)
            {
                case ModalityType.Plan:
                {
                    var rtPlan = (RtPlan)instance;
                    if (!rtPlan.UsesVarianTreatmentUnit)
                    {
                        m_logger.LogError(fileName);
                        m_logger.LogError($"Only Varian treatment units are supported. (Manufacturer={rtPlan.TreatmentMachineManufacturer})");
                        return DicomStatus.Cancel;
                    }

                    var dicomFile = await DicomFile.OpenAsync(fileName);
                    var dataset = dicomFile.Dataset;
                    if (rtPlan.MapMachineIfNeeded(dataset))
                    {
                        var tempFileName = Path.GetTempPath() + Guid.NewGuid() + ".dcm";
                        await dicomFile.SaveAsync(tempFileName, new DicomWriteOptions { ExplicitLengthSequences = true, ExplicitLengthSequenceItems = true, KeepGroupLengths = true });
                        try
                        {
                            dicomStatus = await SendFile(tempFileName);
                        }
                        finally
                        {
                            File.Delete(tempFileName);
                        }
                    }
                    else
                        dicomStatus = await SendFile(fileName);

                    break;
                }
                default:
                    dicomStatus = await SendFile(fileName);
                    break;
            }

            if (dicomStatus == DicomStatus.Success)
            {
                m_console.Out.WriteLine($"Stored {fileName}");
                if (statusLines != null)
                    statusLines.Add(fileName);
                if (!string.IsNullOrEmpty(statusFileName))
                    await File.AppendAllTextAsync(statusFileName, fileName + Environment.NewLine);
            }
            else
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Failed to store {fileName}, Status = {dicomStatus}.");
                stringBuilder.AppendLine(instance.ToString());
                m_logger.LogError(stringBuilder.ToString());
            }

            return dicomStatus;
        }

        private async Task<DicomStatus> SendFile(string fileName)
        {
            var request = new DicomCStoreRequest(fileName);

            var status = DicomStatus.Warning;
            request.OnResponseReceived += (req, response) => status = response.Status;

            await m_client.AddRequestAsync(request);
            await m_client.SendAsync();

            return status;
        }

        private readonly IDicomClient m_client;

        private readonly ILogger m_logger;

        private readonly IConsole m_console;
    }
}
