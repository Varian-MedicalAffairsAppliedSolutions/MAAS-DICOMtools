using FellowOakDicom;
using System.Text;

namespace DicomTools.DataModel
{
    public class Series
    {
        private Series(DicomDataset dataset)
        {
            PatientId = dataset.GetSingleValue<string>(DicomTag.PatientID);
            StudyInstanceUid = dataset.GetSingleValue<DicomUID>(DicomTag.StudyInstanceUID);
            InstanceUid = dataset.GetSingleValue<DicomUID>(DicomTag.SeriesInstanceUID);
            Number = dataset.GetSingleValueOrDefault<int?>(DicomTag.SeriesNumber, null);
            Description = dataset.GetSingleValueOrDefault<string?>(DicomTag.SeriesDescription, null);
            Date = dataset.GetSingleValueOrDefault<DateTime?>(DicomTag.SeriesDate, null);

            var modality = dataset.GetSingleValue<string>(DicomTag.Modality);
            Modality = Modality.CreateFromModalityString(modality);
        }

        public string PatientId { get; }

        public DicomUID StudyInstanceUid { get; }

        public DicomUID InstanceUid { get; }

        public int? Number { get; }

        public string? Description { get; }

        public DateTime? Date { get; }

        public Modality Modality { get; }

        public static Series Create(DicomDataset dataset)
        {
            return new Series(dataset);
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"PatientId: {PatientId}");
            stringBuilder.AppendLine($"StudyInstanceUID: {StudyInstanceUid.UID}");
            stringBuilder.AppendLine($"InstanceUID: {InstanceUid.UID}");
            if (Number != null)
                stringBuilder.AppendLine($"Number: {Number}");
            if (Description != null)
                stringBuilder.AppendLine($"Description: {Description}");
            if (Date != null)
                stringBuilder.AppendLine($"Date: {Date}");

            stringBuilder.AppendLine($"SOPClass: {Modality}");
            return stringBuilder.ToString();
        }
    }
}
