using FellowOakDicom;

namespace DicomTools.DataModel
{
    public enum ModalityType
    {
        Unknown,
        Plan,
        StructureSet,
        Dose,
        CtImage,
        PetImage,
        MrImage,
        RtImage,
        Registration,
        TreatmentRecord
    }

    public class Modality(ModalityType type)
    {
        protected bool Equals(Modality? other)
        {
            return Type == other?.Type;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Modality)obj);
        }

        public override int GetHashCode()
        {
            return (int)Type;
        }

        public ModalityType Type { get; } = type;

        public static Modality Create(DicomUID classUid)
        {
            if (classUid == DicomUID.RTPlanStorage)
                return new Modality(ModalityType.Plan);
            if (classUid.UID == "1.2.246.352.70.1.70") // Halcyon
                return new Modality(ModalityType.Plan);
            if (classUid == DicomUID.RTStructureSetStorage)
                return new Modality(ModalityType.StructureSet);
            if (classUid == DicomUID.RTDoseStorage)
                return new Modality(ModalityType.Dose);
            if (classUid == DicomUID.CTImageStorage)
                return new Modality(ModalityType.CtImage);
            if (classUid == DicomUID.PositronEmissionTomographyImageStorage)
                return new Modality(ModalityType.PetImage);
            if (classUid == DicomUID.MRImageStorage)
                return new Modality(ModalityType.MrImage);
            if (classUid == DicomUID.RTImageStorage)
                return new Modality(ModalityType.RtImage);
            if (classUid == DicomUID.SpatialRegistrationStorage)
                return new Modality(ModalityType.Registration);
            if (classUid == DicomUID.RTBeamsTreatmentRecordStorage)
                return new Modality(ModalityType.TreatmentRecord);
            return new Modality(ModalityType.Unknown);
        }

        public override string ToString() => Type switch
        {
            ModalityType.CtImage => "CT",
            ModalityType.Plan => "RT-PLAN",
            ModalityType.StructureSet => "RT-STRUCT",
            ModalityType.Dose => "RT-DOSE",
            ModalityType.PetImage => "PET",
            ModalityType.MrImage => "MR",
            ModalityType.RtImage => "RT-IMAGE",
            ModalityType.Registration => "REG",
            ModalityType.TreatmentRecord => "RTRECORD",
            _ => "Unknown"
        };

        public static Modality CreateFromModalityString(string modalityString)
        {
            if (modalityString == "RTPLAN")
                return new Modality(ModalityType.Plan);
            if (modalityString == "RTSTRUCT")
                return new Modality(ModalityType.StructureSet);
            if (modalityString == "RTDOSE")
                return new Modality(ModalityType.Dose);
            if (modalityString == "CT")
                return new Modality(ModalityType.CtImage);
            if (modalityString == "PET")
                return new Modality(ModalityType.PetImage);
            if (modalityString == "MR")
                return new Modality(ModalityType.MrImage);
            if (modalityString == "RTIMAGE")
                return new Modality(ModalityType.RtImage);
            if (modalityString == "REG")
                return new Modality(ModalityType.Registration);
            if (modalityString == "RTRECORD")
                return new Modality(ModalityType.TreatmentRecord);
            return new Modality(ModalityType.Unknown);
        }
    }
}
