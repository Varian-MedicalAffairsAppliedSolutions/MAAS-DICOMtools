using System.CommandLine;
using System.CommandLine.IO;
using DicomTools.CommandLineExtensions;
using Microsoft.Extensions.Logging;

namespace DicomTools.Retrieve
{
    public class RetrieveCommandHandler : ICommandOptionsHandler<RetrieveOptions>
    {
        public RetrieveCommandHandler(ILogger<RetrieveCommandHandler> logger,
            IConsole console,
            IConfirmationService confirmationService,
            StoreService storeService,
            RetrieveOptions globalRetrieveOptions)
        {
            m_logger = logger;
            m_console = console;
            m_confirmationService = confirmationService;
            m_storeService = storeService;
            m_globalRetrieveOptions = globalRetrieveOptions;
        }

        public async Task<int> HandleAsync(RetrieveOptions options, CancellationToken cancellationToken)
        {
            // Copy RetrieveOptions to "global"
            options.CopyTo(m_globalRetrieveOptions);

            if (Directory.Exists(options.Path))
            {
                if (!m_confirmationService.Confirm($"Directory {options.Path} exist. Do you want to continue (y=yes)?"))
                    return 1;
            }

            var useGet = options.UseGet;
            var patientId = options.PatientId;
            var queryRetrieve = new DicomQueryRetrieve(m_logger, m_storeService, options);
            var studies = await queryRetrieve.FindStudies(patientId);

            foreach (var study in studies)
            { 
                m_console.Out.WriteLine($"Found study: {study}");

                if (useGet)
                {
                    var seriesList = await queryRetrieve.FindSeries(study);
                    foreach (var series in seriesList)
                    {
                        await queryRetrieve.Get(series);
                    }
                }
                else
                    await queryRetrieve.Move(study);
            }

            return 0;
        }

        private readonly ILogger<RetrieveCommandHandler> m_logger;

        private readonly IConsole m_console;

        private readonly IConfirmationService m_confirmationService;

        private readonly StoreService m_storeService;

        private readonly RetrieveOptions m_globalRetrieveOptions;
    }
}
