using DicomTools.CommandLineExtensions;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.IO;
using DicomTools.DataModel.CollectFiles;
using DicomTools.DataModel.ReferenceTree;

namespace DicomTools.Show
{
    public class ShowCommandHandler(ILogger<ShowCommandHandler> m_logger, IConsole m_console) : ICommandOptionsHandler<ShowOptions>
    {
        public async Task<int> HandleAsync(ShowOptions options, CancellationToken cancellationToken)
        {
            var path = options.Path;
            var searchPattern = options.SearchPattern;

            var defaultMachinesByModel = new Dictionary<string, string>();
            foreach (var defaultMachine in options.DefaultMachines)
            {
                var keyValuePair = defaultMachine.Split('=');
                defaultMachinesByModel.Add(keyValuePair[0], keyValuePair[1]);
            }

            try
            {
                var machineMapping = new Dictionary<string, string>();
                var collectedPatientSeries = FileCollector.CollectFiles(m_logger, m_console, path, searchPattern, machineMapping, defaultMachinesByModel);

                if (options.Format == "flat")
                    ShowFlat(collectedPatientSeries);
                else
                    ShowTree(collectedPatientSeries);

                return await Task.FromResult(0);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex.Message);
                return await Task.FromResult(1);
            }
        }

        private void ShowTree(CollectedPatientSeries collectedPatientSeries)
        {
            foreach (var patientSeries in collectedPatientSeries)
            {
                m_console.Out.WriteLine($"Patient: {patientSeries.PatientId}");

                var referenceTree = ReferenceTree.Create(m_logger, m_console, patientSeries.CollectedSeries);
                m_console.Out.WriteLine(referenceTree.ToString());
            }
        }

        private void ShowFlat(CollectedPatientSeries collectedPatientSeries)
        {
            foreach (var patientSeries in collectedPatientSeries)
            {
                m_console.Out.WriteLine($"Patient: {patientSeries.PatientId}");
                foreach (var series in patientSeries.CollectedSeries)
                {
                    m_console.Out.WriteLine($"Series: {series.Modality}, {series.Instances.Count} instances, {series.SeriesUid.UID}.");

                    foreach (var instance in series.Instances)
                    {
                        m_console.Out.WriteLine($"  {instance.InstanceAndFileName.Instance.Label,-24} {instance.InstanceAndFileName.FileName}");
                    }
                }
            }
        }
    }
}