using System.CommandLine;
using System.CommandLine.IO;
using DicomTools.CommandLineExtensions;
using DicomTools.DataModel.CollectFiles;
using DicomTools.DataModel.ReferenceTree;
using Microsoft.Extensions.Logging;

namespace DicomTools.Store
{
    public class StoreCommandHandler(ILogger<StoreCommandHandler> m_logger, IConsole m_console) : ICommandOptionsHandler<StoreOptions>
    {
        public async Task<int> HandleAsync(StoreOptions options, CancellationToken cancellationToken)
        {
            try
            {
                var machineMapping = new Dictionary<string, string>();
                foreach (var mapping in options.MachineMapping)
                {
                    var keyValuePair = mapping.Split('=');
                    machineMapping.Add(keyValuePair[0], keyValuePair[1]);
                }
                var defaultMachinesByModel = new Dictionary<string, string>();
                foreach (var defaultMachine in options.DefaultMachines)
                {
                    var keyValuePair = defaultMachine.Split('=');
                    defaultMachinesByModel.Add(keyValuePair[0], keyValuePair[1]);
                }

                var collectedPatientSeries = FileCollector.CollectFiles(m_logger, m_console, options.Path, options.SearchPattern, machineMapping, defaultMachinesByModel);

                var dicomStore = new DicomStore(m_logger, m_console, options.HostName, options.HostPort, options.CallingAet, options.CalledAet);
                foreach (var patientSeries in collectedPatientSeries)
                {
                    var referenceTree = ReferenceTree.Create(m_logger, m_console, patientSeries.CollectedSeries);
                    try
                    {
                        m_console.Out.WriteLine($"Patient: {patientSeries.PatientId}");
                        m_console.Out.WriteLine(referenceTree.ToString());
                        await dicomStore.SendReferenceTree(referenceTree, options.StatusFileName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }

                return await Task.FromResult(0);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex.Message);
                return await Task.FromResult(1);
            }
        }
    }
}
