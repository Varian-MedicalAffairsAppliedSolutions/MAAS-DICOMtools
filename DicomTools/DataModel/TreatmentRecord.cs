using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class TreatmentRecord : Instance
    {
        private TreatmentRecord(DicomDataset dataset, InstanceReference referencedPlan, int referencedBeamNumber, string beamName, int currentFractionNumber)
            : base(dataset, ModalityType.TreatmentRecord)
        {
            Label = $"Beam {referencedBeamNumber} ({beamName}), fraction {currentFractionNumber}";
            ReferencedPlan = referencedPlan;
            ReferencedBeamNumber = referencedBeamNumber;
            BeamName = beamName;
            CurrentFractionNumber = currentFractionNumber;
        }

        public override string Label { get; }

        public InstanceReference ReferencedPlan { get; }

        public int ReferencedBeamNumber { get; }

        public string BeamName { get; }

        public int CurrentFractionNumber { get; }

        public static TreatmentRecord Create(DicomDataset dataset)
        {
            var referencedPlanSequence = dataset.GetSequence(DicomTag.ReferencedRTPlanSequence);
            var referencedPlanUid = referencedPlanSequence.Items.Single()
                .GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID);

            var treatmentSessionBeamSequence = dataset.GetSequence(DicomTag.TreatmentSessionBeamSequence);
            var firstTreatmentSessionBeam = treatmentSessionBeamSequence.Items.First();
            var referencedBeamNumber = firstTreatmentSessionBeam.GetSingleValue<int>(DicomTag.ReferencedBeamNumber);
            var beamName = firstTreatmentSessionBeam.GetSingleValue<string>(DicomTag.BeamName);
            var currentFractionNumber = firstTreatmentSessionBeam.GetSingleValue<int>(DicomTag.CurrentFractionNumber);

            return new TreatmentRecord(dataset, new InstanceReference(new Modality(ModalityType.Plan), referencedPlanUid),
                referencedBeamNumber, beamName, currentFractionNumber);
        }
    }
}
