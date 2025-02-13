using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class MrImage : Image
    {
        private MrImage(DicomDataset dataset)
            : base(dataset, ModalityType.MrImage)
        {
            Label = dataset.GetSingleValueOrDefault(DicomTag.InstanceNumber, string.Empty);
        }

        public override string Label { get; }

        public static MrImage Create(DicomDataset dataset)
        {
            return new MrImage(dataset);
        }
    }
}
