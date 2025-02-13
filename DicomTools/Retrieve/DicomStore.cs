using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using System.Text;
using FellowOakDicom;

namespace DicomTools.Retrieve
{
    public class DicomStore : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        public DicomStore(INetworkStream stream, Encoding fallbackEncoding, ILogger logger, DicomServiceDependencies dependencies)
            : base(stream, fallbackEncoding, logger, dependencies)
        {
            m_logger = logger;
        }

        private DicomStoreService Service => (DicomStoreService) UserState;

        public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }

        public void OnConnectionClosed(Exception exception)
        {
            // Empty
        }

        // Anonymization works per study.
        public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
        {
            var studyUid = request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID).Trim();
            var instanceUid = request.SOPInstanceUID.UID;

            m_logger.LogDebug($"Got CStoreRequest for Study={studyUid}, Instance={instanceUid}");

            return await Service.OnCStoreRequestAsync(request);
        }

        public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
        {
            // Let library handle logging and error response.
            return Task.CompletedTask;
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            // Empty
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return SendAssociationReleaseResponseAsync();
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            var callingAe = association.CallingAE;
            var calledAe = association.CalledAE;

            Logger.LogInformation($"Received association request from AE: {callingAe} with IP: {association.RemoteHost} ");

#if false
            if (Service.Configuration.RetrieveOptions?.CalledAet != callingAe)
            {
                Logger.LogError($"Association with {callingAe} rejected since called aet {calledAe} is unknown");
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CallingAENotRecognized);
            }

            if (Service.Configuration.RetrieveOptions?.CallingAet != calledAe)
            {
                Logger.LogError($"Association with {calledAe} rejected since calling aet {callingAe} is unknown");
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }
#endif

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification)
                {
                    pc.AcceptTransferSyntaxes(s_acceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                {
                    pc.AcceptTransferSyntaxes(s_acceptedImageTransferSyntaxes);
                }
            }

            return SendAssociationAcceptAsync(association);
        }

        private static readonly DicomTransferSyntax[] s_acceptedTransferSyntaxes =
        [
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian
        ];

        private static readonly DicomTransferSyntax[] s_acceptedImageTransferSyntaxes =
        [
            // Lossless
            DicomTransferSyntax.JPEGLSLossless,
            DicomTransferSyntax.JPEG2000Lossless,
            DicomTransferSyntax.JPEGProcess14SV1,
            DicomTransferSyntax.JPEGProcess14,
            DicomTransferSyntax.RLELossless,
            // Lossy
            DicomTransferSyntax.JPEGLSNearLossless,
            DicomTransferSyntax.JPEG2000Lossy,
            DicomTransferSyntax.JPEGProcess1,
            DicomTransferSyntax.JPEGProcess2_4,
            // Uncompressed
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian
        ];

        private readonly ILogger m_logger;
    }
}
