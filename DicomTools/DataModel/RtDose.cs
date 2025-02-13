using FellowOakDicom;
using System.Text;

namespace DicomTools.DataModel
{
    public class RtDose : Instance
    {
        private RtDose(DicomDataset dataset, InstanceReference referencedPlan, InstanceReference? referencedStructureSet)
            : base(dataset, ModalityType.Dose)
        {
            ReferencedPlan = referencedPlan;
            ReferencedStructureSet = referencedStructureSet;
            Label = InstanceUid.UID;
            SummationType = dataset.GetSingleValueOrDefault(DicomTag.DoseSummationType, string.Empty);
        }

        public override string Label { get; }

        public string SummationType { get; }

        public InstanceReference ReferencedPlan { get; }

        public InstanceReference? ReferencedStructureSet { get; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder(base.ToString());
            stringBuilder.AppendLine($"ReferencedPlan: {ReferencedPlan.InstanceUid.UID}");
            if (ReferencedStructureSet != null)
                stringBuilder.AppendLine($"ReferencedStructureSet: {ReferencedStructureSet.InstanceUid.UID}");
            return stringBuilder.ToString();
        }

        public static RtDose Create(DicomDataset dataset)
        {
            var referencedPlanSequence = dataset.GetSequence(DicomTag.ReferencedRTPlanSequence);
            var referencedPlanUid = referencedPlanSequence.Items.Single()
                .GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID);
            var referencedPlan = new InstanceReference(new Modality(ModalityType.Plan), referencedPlanUid);

            InstanceReference? referencedStructureSet = null;
            if (dataset.TryGetSequence(DicomTag.ReferencedStructureSetSequence, out var referencedStructureSetSequence))
            {
                var referencedStructureSetUid = referencedStructureSetSequence.Items.Single()
                    .GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID);
                referencedStructureSet = new InstanceReference(new Modality(ModalityType.StructureSet), referencedStructureSetUid);
            }

            return new RtDose(dataset, referencedPlan, referencedStructureSet);
        }
    }
}
