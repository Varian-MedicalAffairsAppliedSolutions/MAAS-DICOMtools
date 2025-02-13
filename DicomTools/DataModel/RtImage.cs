using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class RtImage : Image
    {
        private RtImage(DicomDataset dataset, InstanceReference? referencedPlan)
            : base(dataset, ModalityType.RtImage)
        {
            ReferencedPlan = referencedPlan;
            ReferencedBeamNumber = dataset.GetSingleValueOrDefault<int?>(DicomTag.ReferencedBeamNumber, null);
            Label = dataset.GetSingleValueOrDefault(DicomTag.RTImageLabel, string.Empty);
            InstanceNumber = dataset.GetSingleValue<int>(DicomTag.InstanceNumber);
        }

        public override string Label { get; }

        public int? ReferencedBeamNumber { get; }

        public int InstanceNumber { get; }

        /// <summary>
        /// RTImage either references to a RtPlan or plan references to RtImage.
        /// </summary>
        public InstanceReference? ReferencedPlan { get; }

        public static RtImage Create(DicomDataset dataset)
        {
            InstanceReference? referencedPlan = null;
            if (dataset.TryGetSequence(DicomTag.ReferencedRTPlanSequence, out var referencedPlanSequence))
            {
                var referencedPlanUid = referencedPlanSequence.Items.Single()
                    .GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID);
                referencedPlan = new InstanceReference(new Modality(ModalityType.Plan), referencedPlanUid);
            }

            return new RtImage(dataset, referencedPlan);
        }
    }
}
