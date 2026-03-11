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

            string treatmentMachineManufacturer;
            string? treatmentMachineModel;
            string? treatmentMachineName;
            string? originalTreatmentMachineName = null;
            var beams = new List<RtBeam>();

            if (IsBrachyPlan(dataset))
            {
                // Brachy
                var treatmentMachine = dataset.GetSequence(DicomTag.TreatmentMachineSequence).Single();
                treatmentMachineManufacturer = treatmentMachine.GetSingleValueOrDefault(DicomTag.Manufacturer, string.Empty);
                treatmentMachineModel = treatmentMachine.GetSingleValueOrDefault<string?>(DicomTag.ManufacturerModelName, null);
                treatmentMachineName = treatmentMachine.GetSingleValueOrDefault(DicomTag.TreatmentMachineName, string.Empty);
            }
            else
            {
                // External
                var beamSequence = dataset.GetSequence(DicomTag.BeamSequence);
                var firstBeam = beamSequence.First();
                treatmentMachineManufacturer = firstBeam.GetSingleValueOrDefault(DicomTag.Manufacturer, string.Empty);
                if (string.IsNullOrEmpty(treatmentMachineManufacturer))
                    treatmentMachineManufacturer = "Varian Medical Systems";
                treatmentMachineModel = firstBeam.GetSingleValueOrDefault<string?>(DicomTag.ManufacturerModelName, null);
                treatmentMachineName = firstBeam.GetSingleValueOrDefault(DicomTag.TreatmentMachineName, string.Empty);

                foreach (var beamDataset in beamSequence)
                {
                    beams.Add(RtBeam.Create(beamDataset));
                }
            }

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

            return new RtPlan(dataset, referencedStructureSet, beams,
                treatmentMachineManufacturer, treatmentMachineModel, treatmentMachineName, originalTreatmentMachineName);
        }

        public bool MapMachineIfNeeded(DicomDataset dataset)
        {
            if (OriginalTreatmentMachineName != null)
            {
                if (IsBrachyPlan(dataset))
                {
                    var treatmentMachine = dataset.GetSequence(DicomTag.TreatmentMachineSequence).Single();
                    treatmentMachine.AddOrUpdate(DicomTag.TreatmentMachineName, TreatmentMachineName);
                }
                else
                {
                    var beamSequence = dataset.GetSequence(DicomTag.BeamSequence);
                    foreach (var beam in beamSequence)
                        beam.AddOrUpdate(DicomTag.TreatmentMachineName, TreatmentMachineName);
                }
                return true;
            }

            return false;
        }

        private static bool IsBrachyPlan(DicomDataset dataset)
        {
            return dataset.TryGetSequence(DicomTag.ApplicationSetupSequence, out var _);
        }
    }
}
