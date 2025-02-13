using System.CommandLine;
using System.CommandLine.IO;
using DicomTools.DataModel;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;

namespace DicomTools.Retrieve
{
    internal class DicomQueryRetrieve
    {
        internal DicomQueryRetrieve(ILogger logger, IConsole console, string hostName, int hostPort, string callingAet, string calledAet)
        {
            m_logger = logger;
            m_console = console;

            m_client = DicomClientFactory.Create(hostName, hostPort, useTls: false, callingAe: callingAet, calledAe: calledAet);
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
                    // TODO: Adapt
                    m_logger.LogError($"Patient" +
                              $"{response.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty)}," +
                              $"{(response.Dataset.TryGetString(DicomTag.ModalitiesInStudy, out var dummy)
                                  ? dummy : string.Empty)}-Study from" +
                              $"{response.Dataset.GetSingleValueOrDefault(DicomTag.StudyDate, new DateTime())} with UID" +
                              $"{response.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty)} ");
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
                    m_console.Out.WriteLine(series.ToString());
                }
                else if (response.Status == DicomStatus.Success)
                {
                    // Do nothing, finished
                }
                else
                {
                    // TODO: Adapt
                    m_logger.LogError($"Patient" +
                              $"{response.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty)}," +
                              $"{(response.Dataset.TryGetString(DicomTag.ModalitiesInStudy, out var dummy)
                                  ? dummy : string.Empty)}-Study from" +
                              $"{response.Dataset.GetSingleValueOrDefault(DicomTag.StudyDate, new DateTime())} with UID" +
                              $"{response.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty)} ");
                }
            };
            await m_client.AddRequestAsync(findRequest);
            await m_client.SendAsync();

            return listOfSeries;
        }

        public async Task<int> Move(Study study)
        {
            var moveRequest = new DicomCMoveRequest(m_client.CallingAe, study.InstanceUid.UID);
            var completedRequests = 0;
            moveRequest.OnResponseReceived += (request, response) =>
            {
                completedRequests = response.Completed;
                m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
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
                m_logger.LogDebug($"Completed {response.Completed}, Remaining {response.Remaining}");
            };
            await m_client.AddRequestAsync(moveRequest);
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

        private readonly IConsole m_console;

        private readonly IDicomClient m_client;
    }
}
