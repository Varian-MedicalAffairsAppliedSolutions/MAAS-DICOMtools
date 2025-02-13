using FellowOakDicom;
using System.Text;

namespace DicomTools.DataModel
{
    public class RtPlan : Instance
    {
        private RtPlan(DicomDataset dataset, InstanceReference? referencedStructureSet, IReadOnlyList<RtBeam> beams,
            string treatmentMachineManufacturer, string? treatmentMachineModel, string treatmentMachineName, string? originalTreatmentMachineName)
            : base(dataset, ModalityType.Plan)
        {
            ReferencedStructureSet = referencedStructureSet;
            Beams = beams;
            TreatmentMachineManufacturer = treatmentMachineManufacturer;
            TreatmentMachineModel = treatmentMachineModel;
            TreatmentMachineName = treatmentMachineName;
            OriginalTreatmentMachineName = originalTreatmentMachineName;
            Label = dataset.GetSingleValueOrDefault(DicomTag.RTPlanLabel, string.Empty);
            PlanIntent = dataset.GetSingleValueOrDefault(DicomTag.PlanIntent, string.Empty);
            ApprovalStatus = ApprovalStatusExtensions.FromDicomValue(dataset.GetSingleValueOrDefault(DicomTag.ApprovalStatus, string.Empty));
        }

        public override string Label { get; }

        public string PlanIntent { get; }

        public ApprovalStatus ApprovalStatus { get; }

        public InstanceReference? ReferencedStructureSet { get; }

        public IReadOnlyList<RtBeam> Beams { get; }

        public string TreatmentMachineManufacturer { get; }

        public string? TreatmentMachineModel { get; }

        public string TreatmentMachineName { get; }

        public string? OriginalTreatmentMachineName { get; }

        public bool UsesVarianTreatmentUnit =>
            TreatmentMachineManufacturer.StartsWith("varian", StringComparison.InvariantCultureIgnoreCase);

        public override string ToString()
        {
            var stringBuilder = new StringBuilder(base.ToString());
            if (ReferencedStructureSet != null)
                stringBuilder.AppendLine($"ReferencedStructureSet: {ReferencedStructureSet.InstanceUid.UID}");
            else
                stringBuilder.AppendLine("ReferencedStructureSet: None");
            if (TreatmentMachineModel != null)
                stringBuilder.AppendLine($"TreatmentMachineModel: {TreatmentMachineModel}");
            stringBuilder.AppendLine($"TreatmentMachineName: {TreatmentMachineName}");
            if (OriginalTreatmentMachineName != null)
                stringBuilder.AppendLine($"OriginalTreatmentMachineName: {OriginalTreatmentMachineName}");
            return stringBuilder.ToString();
        }

        public static RtPlan Create(DicomDataset dataset, IReadOnlyDictionary<string, string> machineMapping, IReadOnlyDictionary<string, string> defaultMachinesByModel)
        {
            DicomUID? referencedStructureSetUid = null;
            if (dataset.TryGetSequence(DicomTag.ReferencedStructureSetSequence, out var referencedStructureSetSequence))
            {
                referencedStructureSetUid = referencedStructureSetSequence.Items.Single()
                    .GetSingleValue<DicomUID>(DicomTag.ReferencedSOPInstanceUID);
            }

            var referencedStructureSet = referencedStructureSetUid != null ? new InstanceReference(new Modality(ModalityType.StructureSet), referencedStructureSetUid) : null;
            var beamSequence = dataset.GetSequence(DicomTag.BeamSequence);
            var firstBeam = beamSequence.First();
            var treatmentMachineManufacturer = firstBeam.GetSingleValueOrDefault(DicomTag.Manufacturer, string.Empty);
            if (string.IsNullOrEmpty(treatmentMachineManufacturer))
                treatmentMachineManufacturer = "Varian Medical Systems";
            var treatmentMachineModel = firstBeam.GetSingleValueOrDefault<string?>(DicomTag.ManufacturerModelName, null);
            var treatmentMachineName = firstBeam.GetSingleValueOrDefault(DicomTag.TreatmentMachineName, string.Empty);
            string? originalTreatmentMachineName = null;
            if (string.IsNullOrEmpty(treatmentMachineName))
            {
                if (string.IsNullOrEmpty(treatmentMachineModel))
                    throw new ApplicationException("ManufacturerModelName and TreatmentMachineName are both missing. ");
                if (!defaultMachinesByModel.TryGetValue(treatmentMachineModel, out treatmentMachineName))
                    throw new ApplicationException(
                        $"You need to give default machine name for {treatmentMachineModel} machines.");
                originalTreatmentMachineName = treatmentMachineName;
            }
            if (machineMapping.TryGetValue(treatmentMachineName, out var mappedTreatmentMachineName))
            {
                originalTreatmentMachineName = treatmentMachineName;
                treatmentMachineName = mappedTreatmentMachineName;
            }

            var beams = new List<RtBeam>();
            foreach (var beamDataset in beamSequence)
            {
                beams.Add(RtBeam.Create(beamDataset));
            }

            return new RtPlan(dataset, referencedStructureSet, beams,
                treatmentMachineManufacturer, treatmentMachineModel, treatmentMachineName, originalTreatmentMachineName);
        }

        public bool MapMachineIfNeeded(DicomDataset dataset)
        {
            var beamSequence = dataset.GetSequence(DicomTag.BeamSequence);
            if (OriginalTreatmentMachineName != null)
            {
                foreach (var beam in beamSequence)
                    beam.AddOrUpdate(DicomTag.TreatmentMachineName, TreatmentMachineName);
                return true;
            }

            return false;
        }
    }
}
