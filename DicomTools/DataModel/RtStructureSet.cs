using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class RtStructureSet : Instance
    {
        private RtStructureSet(DicomDataset dataset, IReadOnlyList<InstanceReference> referencedImages)
            : base(dataset, ModalityType.StructureSet)
        {
            ReferencedImages = referencedImages;
            Label = dataset.GetSingleValueOrDefault(DicomTag.StructureSetLabel, string.Empty);
            if (dataset.TryGetSingleValue<DicomUID>(DicomTag.FrameOfReferenceUID, out var frameOfReferenceUid))
            {
                FrameOfReferenceUid = frameOfReferenceUid;
            }
            else
            {
                var sequence = dataset.GetSequence(DicomTag.ReferencedFrameOfReferenceSequence);
                var sequenceItem = sequence.Items.Single();  // Should have only one.
                FrameOfReferenceUid = sequenceItem.GetSingleValue<DicomUID>(DicomTag.FrameOfReferenceUID);
            }
        }

        public override string Label { get; }

        public DicomUID FrameOfReferenceUid { get; }

        public IReadOnlyList<InstanceReference> ReferencedImages { get; }

        public static RtStructureSet Create(DicomDataset dataset)
        {
            var referencedImages = new HashSet<InstanceReference>();
            if (dataset.TryGetSequence(DicomTag.ROIContourSequence, out var roiContourDicomSequence))
            {
                foreach (var roiContour in roiContourDicomSequence)
                {
                    if (roiContour.TryGetSequence(DicomTag.ContourSequence, out var contourSequence))
                    {
                        foreach (var contour in contourSequence)
                        {
                            if (contour.TryGetSequence(DicomTag.ContourImageSequence, out var imageSequence))
                            {
                                foreach (var image in imageSequence)
                                {
                                    var referencedSopClass = image.GetSingleValue<DicomUID>(DicomTag.ReferencedSOPClassUID);
                                    var referencedImageUid = image.GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID);
                                    var imageReference = new InstanceReference(Modality.Create(referencedSopClass), referencedImageUid);
                                    referencedImages.Add(imageReference);
                                }
                            }
                        }
                    }
                }
            }

            var referencedImagesList = new List<InstanceReference>();
            referencedImagesList.AddRange(referencedImages);

            return new RtStructureSet(dataset, referencedImagesList);
        }
    }
}
