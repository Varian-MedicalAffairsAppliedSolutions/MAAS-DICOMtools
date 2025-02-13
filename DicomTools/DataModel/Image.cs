using FellowOakDicom;

namespace DicomTools.DataModel
{
    public abstract class Image(DicomDataset dataset, ModalityType modality) : Instance(dataset, modality)
    {
        public string[] ImageType { get; } = dataset.GetValues<string>(DicomTag.ImageType);

        public DicomUID FrameOfReferenceUid { get; } = dataset.GetSingleValue<DicomUID>(DicomTag.FrameOfReferenceUID);
    }
}
