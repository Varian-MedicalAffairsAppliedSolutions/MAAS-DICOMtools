using DicomTools.Configuration;
using FellowOakDicom.Network;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DicomTools.Retrieve
{
    public class DicomStoreHostedService : IHostedService
    {
        public DicomStoreHostedService(IConfiguration configuration, ILogger<DicomStoreHostedService> logger, StoreService storeService, IHostApplicationLifetime applicationLifetime)
        {
            m_logger = logger;
            m_storeService = storeService;
            m_applicationLifetime = applicationLifetime;
            m_dicomStorageConfiguration = configuration.GetRequiredSection("DicomStorage").Get<DicomStorageConfiguration>()!;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var portNumber = m_dicomStorageConfiguration.PortNumber;
            m_dicomServer = DicomServerFactory.Create<DicomStoreService>(portNumber, logger: m_logger, userState: m_storeService);

            m_applicationLifetime.ApplicationStopping.Register(m_storeService.ProcessCollectedSeries);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            m_dicomServer?.Stop();
            m_dicomServer?.Dispose();
            m_dicomServer = null;

            return Task.CompletedTask;
        }

        private readonly DicomStorageConfiguration m_dicomStorageConfiguration;

        private readonly ILogger m_logger;

        private readonly IHostApplicationLifetime m_applicationLifetime;

        private IDicomServer? m_dicomServer;

        private readonly StoreService m_storeService;
    }
}
