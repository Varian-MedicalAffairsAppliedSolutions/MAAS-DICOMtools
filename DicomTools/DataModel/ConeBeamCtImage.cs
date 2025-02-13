using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class ConeBeamCtImage : CtImage
    {
        private ConeBeamCtImage(DicomDataset dataset, InstanceReference referencedPlan)
            : base(dataset)
        {
            ReferencedPlan = referencedPlan;
        }

        public InstanceReference ReferencedPlan { get; }

        public new static ConeBeamCtImage Create(DicomDataset dataset)
        {
            var referencedInstanceSequence = dataset.GetSequence(DicomTag.ReferencedInstanceSequence);

            var firstReference = referencedInstanceSequence.First();

            var referencedPlanUid = firstReference.GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID);
            var referencedPlan = new InstanceReference(new Modality(ModalityType.Plan), referencedPlanUid);

            return new ConeBeamCtImage(dataset, referencedPlan);
        }
    }
}
