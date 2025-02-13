using System.ComponentModel;
using System.Text;
using FellowOakDicom;

namespace DicomTools.DataModel
{
    public abstract class Instance
    {
        internal Instance(DicomDataset dataset, ModalityType modality)
        {
            PatientId = dataset.GetSingleValue<string>(DicomTag.PatientID);
            Modality = new Modality(modality);
            InstanceUid = dataset.GetSingleValue<DicomUID>(DicomTag.SOPInstanceUID);
            StudyUid = dataset.GetSingleValue<DicomUID>(DicomTag.StudyInstanceUID);
            SeriesUid = dataset.GetSingleValue<DicomUID>(DicomTag.SeriesInstanceUID);
        }

        public string PatientId { get; }

        public Modality Modality { get; }

        public DicomUID InstanceUid { get; }

        public DicomUID StudyUid { get; }

        public DicomUID SeriesUid { get; }

        public abstract string Label { get; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Storage: {Modality}");
            stringBuilder.AppendLine($"StudyUID: {StudyUid.UID}");
            stringBuilder.AppendLine($"SeriesUID: {SeriesUid.UID}");
            stringBuilder.AppendLine($"InstanceUID: {InstanceUid.UID}");
            if (!string.IsNullOrEmpty(Label))
                stringBuilder.AppendLine($"Label: {Label}");
            return stringBuilder.ToString();
        }

        public string GenerateFileName() => Modality.Type switch
        {
            ModalityType.CtImage => $"CT{InstanceUid.UID}.dcm",
            ModalityType.Plan => $"RP{InstanceUid.UID}.dcm",
            ModalityType.StructureSet => $"RS{InstanceUid.UID}.dcm",
            ModalityType.Dose => $"RD{InstanceUid.UID}.dcm",
            ModalityType.PetImage => $"PT{InstanceUid.UID}.dcm",
            ModalityType.MrImage => $"MR{InstanceUid.UID}.dcm",
            ModalityType.RtImage => $"RI{InstanceUid.UID}.dcm",
            ModalityType.Registration => $"RE{InstanceUid.UID}.dcm",
            ModalityType.TreatmentRecord => $"RT{InstanceUid.UID}.dcm",
            _ => "Unknown.dcm"
        };

        public static Instance CreateFromDataset(DicomDataset dataset, IReadOnlyDictionary<string, string> machineMapping, IReadOnlyDictionary<string, string> defaultMachinesByModel)
        {
            if (dataset.TryGetSingleValue<DicomUID>(DicomTag.SOPClassUID, out var classUid))
            {
                var modality = Modality.Create(classUid);

                switch (modality.Type)
                {
                    case ModalityType.Plan:
                        return RtPlan.Create(dataset, machineMapping, defaultMachinesByModel);
                    case ModalityType.StructureSet:
                        return RtStructureSet.Create(dataset);
                    case ModalityType.Dose:
                        return RtDose.Create(dataset);
                    case ModalityType.CtImage:
                        if (dataset.TryGetSequence(DicomTag.ReferencedInstanceSequence,
                                out var referencedInstanceSequence))
                        {
                            var firstReference = referencedInstanceSequence.Items.First();
                            var referenceClassUid =
                                firstReference.GetSingleValue<DicomUID>(DicomTag.ReferencedSOPClassUID);
                            if (referenceClassUid == DicomUID.RTPlanStorage)
                            {
                                return ConeBeamCtImage.Create(dataset);
                            }
                        }

                        return CtImage.Create(dataset);
                    case ModalityType.PetImage:
                        return PetImage.Create(dataset);
                    case ModalityType.MrImage:
                        return MrImage.Create(dataset);
                    case ModalityType.RtImage:
                        return RtImage.Create(dataset);
                    case ModalityType.Registration:
                        return Registration.Create(dataset);
                    case ModalityType.TreatmentRecord:
                        return TreatmentRecord.Create(dataset);
                    default:
                        throw new InvalidEnumArgumentException($"{classUid.UID} is not supported",
                            (int) modality.Type, typeof(ModalityType));
                }
            }
            throw new InvalidEnumArgumentException("Dataset is not SOPInstance.");
        }
    }
}
