using FellowOakDicom;
using System.Text;

namespace DicomTools.DataModel
{
    public class Study
    {
        private Study(DicomDataset dataset)
        {
            PatientId = dataset.GetSingleValue<string>(DicomTag.PatientID);
            PatientName = dataset.GetSingleValue<string>(DicomTag.PatientName);
            InstanceUid = dataset.GetSingleValue<DicomUID>(DicomTag.StudyInstanceUID);
            Id = dataset.GetSingleValueOrDefault(DicomTag.StudyID, string.Empty);
            Description = dataset.GetSingleValueOrDefault<string>(DicomTag.StudyDescription, null!);
            Date = dataset.GetSingleValueOrDefault(DicomTag.StudyDate, DateTime.MinValue);
        }

        public string PatientId { get; }

        public string PatientName { get; }

        public DicomUID InstanceUid { get; }

        public string Id { get; }

        public string? Description { get; }

        public DateTime Date { get; }

        public static Study Create(DicomDataset dataset)
        {
            return new Study(dataset);
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"PatientId: {PatientId}");
            stringBuilder.AppendLine($"PatientName: {PatientName}");
            stringBuilder.AppendLine($"InstanceUID: {InstanceUid.UID}");
            stringBuilder.AppendLine($"Id: {Id}");
            if (Description != null)
                stringBuilder.AppendLine($"Description: {Description}");
            stringBuilder.AppendLine($"StudyDate: {Date}");
            return stringBuilder.ToString();
        }
    }
}
