using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class RtBeam
    {
        private RtBeam(DicomDataset dataset, IReadOnlyList<InstanceReference> drrImageReferences)
        {
            Label = dataset.GetSingleValue<string>(DicomTag.BeamName);
            BeamNumber = dataset.GetSingleValue<int>(DicomTag.BeamNumber);
            DrrImageReferences = drrImageReferences;
            TreatmentDeliveryType = dataset.GetSingleValue<string>(DicomTag.TreatmentDeliveryType);
        }

        public  string Label { get; }

        public int BeamNumber { get; }

        public string TreatmentDeliveryType { get; }

        public IReadOnlyList<InstanceReference> DrrImageReferences { get; }

        public static RtBeam Create(DicomDataset dataset)
        {
            var drrImageReferences = new List<InstanceReference>();
            if (dataset.TryGetSequence(DicomTag.ReferencedReferenceImageSequence, out var referencedImageSequence))
            {
                foreach (var referencedImage in referencedImageSequence)
                {
                    var referencedSopClass = referencedImage.GetSingleValue<DicomUID>(DicomTag.ReferencedSOPClassUID);
                    if (referencedSopClass == DicomUID.RTImageStorage)
                    {
                        var referencedRtImageUid = referencedImage.GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID);
                        drrImageReferences.Add(new InstanceReference(new Modality(ModalityType.RtImage), referencedRtImageUid));
                    }
                }
            }

            return new RtBeam(dataset, drrImageReferences);
        }
    }
}
