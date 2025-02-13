using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class PetImage : Image
    {
        private PetImage(DicomDataset dataset)
            : base(dataset, ModalityType.PetImage)
        {
            Label = dataset.GetSingleValueOrDefault(DicomTag.InstanceNumber, string.Empty);
        }

        public override string Label { get; }

        public static PetImage Create(DicomDataset dataset)
        {
            return new PetImage(dataset);
        }
    }
}
