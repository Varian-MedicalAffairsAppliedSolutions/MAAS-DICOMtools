using System.CommandLine;
using System.CommandLine.IO;
using DicomTools.CommandLineExtensions;
using Microsoft.Extensions.Logging;

namespace DicomTools.Retrieve
{
    public class RetrieveCommandHandler(ILogger<RetrieveCommandHandler> m_logger, IConsole m_console, IConfirmationService m_confirmationService,
        DicomStorageRetrieveOptions m_storageOptions) : ICommandOptionsHandler<RetrieveOptions>
    {
        public async Task<int> HandleAsync(RetrieveOptions options, CancellationToken cancellationToken)
        {
            m_storageOptions.CopyFrom(options);

            if (Directory.Exists(m_storageOptions.Path))
            {
                if (!m_confirmationService.Confirm($"Directory {m_storageOptions.Path} exist. Do you want to continue (y=yes)?"))
                    return 1;
            }

            var patientId = options.PatientId;
            var queryRetrieve = new DicomQueryRetrieve(m_logger, m_console, options.HostName, options.HostPort, options.CallingAet, options.CalledAet);
            var studies = await queryRetrieve.FindStudies(patientId);

            foreach (var study in studies)
            { 
                m_console.Out.WriteLine($"Found study: {study}");
                await queryRetrieve.Move(study);
            }

            return 0;
        }
    }
}
