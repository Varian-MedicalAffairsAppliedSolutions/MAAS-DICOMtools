using System.Text;
using DicomTools.DataModel;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;

namespace DicomTools.Retrieve
{
    internal class DicomQueryRetrieve
    {
        internal DicomQueryRetrieve(ILogger logger, StoreService storeService, RetrieveOptions retrieveOptions)
        {
            m_logger = logger;

            m_client = DicomClientFactory.Create(retrieveOptions.HostName, retrieveOptions.HostPort, useTls: false, callingAe: retrieveOptions.CallingAet, calledAe: retrieveOptions.CalledAet);
            if (retrieveOptions.UseGet)
            {
                m_client.AdditionalPresentationContexts.Add(DicomPresentationContext.GetScpRolePresentationContext(DicomUID.CTImageStorage));
                m_client.AdditionalPresentationContexts.Add(DicomPresentationContext.GetScpRolePresentationContext(DicomUID.RTStructureSetStorage));
                m_client.AdditionalPresentationContexts.Add(DicomPresentationContext.GetScpRolePresentationContext(DicomUID.RTPlanStorage));
                m_client.AdditionalPresentationContexts.Add(DicomPresentationContext.GetScpRolePresentationContext(DicomUID.RTDoseStorage));
                m_client.OnCStoreRequest += storeService.OnCStoreRequestAsync;
            }

            m_client.NegotiateAsyncOps();
        }

        internal async Task<List<Study>> FindStudies(string patientId)
        {
            var findRequest = CreateStudyRequestByPatientName(patientId);

            var studies = new List<Study>();
            findRequest.OnResponseReceived += (req, response) =>
            {
                if (response.Status == DicomStatus.Pending)
                {
                    studies.Add(Study.Create(response.Dataset));
                }
                else if (response.Status == DicomStatus.Success)
                {
                    // Do nothing, finished
                }
                else
                {
                    m_logger.LogError(response.ToString());
                }
            };
            await m_client.AddRequestAsync(findRequest);
            await m_client.SendAsync();

            return studies;
        }

        internal async Task<List<Series>> FindSeries(Study study)
        {
            var findRequest = CreateSeriesRequestByStudyUid(study.InstanceUid);

            var listOfSeries = new List<Series>();
            findRequest.OnResponseReceived += (req, response) =>
            {
                if (response.Status == DicomStatus.Pending)
                {
                    var series = Series.Create(response.Dataset);
                    listOfSeries.Add(series);
                }
                else if (response.Status == DicomStatus.Success)
                {
                    // Do nothing, finished
                }
                else
                {
                    m_logger.LogError(response.ToString());
                }
            };
            await m_client.AddRequestAsync(findRequest);
            await m_client.SendAsync();

            if (m_logger.IsEnabled(LogLevel.Information))
            {
                var messageBuilder = new StringBuilder();
                foreach (var series in listOfSeries)
                {
                    messageBuilder.AppendLine($"Found series: {series.Modality}, {series.InstanceUid.UID}");
                }
                m_logger.LogInformation(messageBuilder.ToString());
            }

            return listOfSeries;
        }

        public async Task<int> Move(Study study)
        {
            var moveRequest = new DicomCMoveRequest(m_client.CallingAe, study.InstanceUid.UID);
            var completedRequests = 0;
            moveRequest.OnResponseReceived += (request, response) =>
            {
                if (response.Status == DicomStatus.Pending)
                    m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
                else if (response.Status == DicomStatus.Success)
                    m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
                else
                    m_logger.LogError(response.ToString());
                completedRequests = response.Completed;
            };
            await m_client.AddRequestAsync(moveRequest);
            await m_client.SendAsync();
            return completedRequests;
        }

        public async Task Move(Series series)
        {
            var moveRequest = new DicomCMoveRequest(m_client.CallingAe, series.StudyInstanceUid.UID, series.InstanceUid.UID);
            moveRequest.OnResponseReceived += (request, response) =>
            {
                if (response.Status == DicomStatus.Pending)
                    m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
                else if (response.Status == DicomStatus.Success)
                    m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
                else
                    m_logger.LogError(response.ToString());
            };
            await m_client.AddRequestAsync(moveRequest);
            await m_client.SendAsync();
        }

        public async Task<int> Get(Study study)
        {
            var getRequest = new DicomCGetRequest(study.InstanceUid.UID);
            var completedRequests = 0;
            getRequest.OnResponseReceived += (request, response) =>
            {
                if (response.Status == DicomStatus.Pending)
                    m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
                else if (response.Status == DicomStatus.Success)
                    m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
                else
                    m_logger.LogError(response.ToString());
                completedRequests = response.Completed;
            };

            await m_client.AddRequestAsync(getRequest);
            await m_client.SendAsync();
            return completedRequests;
        }

        public async Task Get(Series series)
        {
            m_logger.LogInformation($"Get series: {series.Modality}, {series.InstanceUid.UID}");

            var getRequest = new DicomCGetRequest(series.StudyInstanceUid.UID, series.InstanceUid.UID);

            getRequest.OnResponseReceived += (request, response) =>
            {
                if (response.Status == DicomStatus.Pending)
                    m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
                else if (response.Status == DicomStatus.Success)
                    m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
                else
                    m_logger.LogError(response.ToString());
            };

            await m_client.AddRequestAsync(getRequest);
            await m_client.SendAsync();
        }

        private static DicomCFindRequest CreateStudyRequestByPatientName(string patientId)
        {
            var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Study);

            // always add the encoding
            request.Dataset.AddOrUpdate(new DicomTag(0x8, 0x5), "ISO_IR 100");

            // add the dicom tags with empty values that should be included in the result of the QR Server
            request.Dataset.AddOrUpdate(DicomTag.PatientName, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientID, "");
            request.Dataset.AddOrUpdate(DicomTag.ModalitiesInStudy, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyDate, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyID, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyDescription, "");

            // add the dicom tags that contain the filter criteria
            request.Dataset.AddOrUpdate(DicomTag.PatientID, patientId);

            return request;
        }

        public static DicomCFindRequest CreateSeriesRequestByStudyUid(DicomUID studyInstanceUid)
        {
            var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Series);

            request.Dataset.AddOrUpdate(new DicomTag(0x8, 0x5), "ISO_IR 100");

            // add the dicom tags with empty values that should be included in the result
            request.Dataset.AddOrUpdate(DicomTag.PatientID, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesDescription, "");
            request.Dataset.AddOrUpdate(DicomTag.SeriesDate, "");
            request.Dataset.AddOrUpdate(DicomTag.Modality, "");
            request.Dataset.AddOrUpdate(DicomTag.NumberOfSeriesRelatedInstances, "");

            // add the dicom tags that contain the filter criteria
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid);

            return request;
        }

        private readonly ILogger m_logger;

        private readonly IDicomClient m_client;
    }
}
