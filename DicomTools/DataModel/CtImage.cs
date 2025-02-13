using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class CtImage : Image
    {
        protected CtImage(DicomDataset dataset)
            : base(dataset, ModalityType.CtImage)
        {
            Label = dataset.GetSingleValueOrDefault(DicomTag.InstanceNumber, string.Empty);
        }

        public override string Label { get; }

        public static CtImage Create(DicomDataset dataset)
        {
            return new CtImage(dataset);
        }
    }
}
