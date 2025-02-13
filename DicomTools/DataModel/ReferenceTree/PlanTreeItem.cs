using DicomTools.DataModel.CollectFiles;
using FellowOakDicom;

namespace DicomTools.DataModel.ReferenceTree
{
    internal class PlanTreeItem : InstanceTreeItem<RtPlan>
    {
        private PlanTreeItem(RtPlan plan, string fileName, IReadOnlyList<BeamTreeItem> beams, StructureSetTreeItem? structureSet, IReadOnlyList<DoseTreeItem> doses,
            IReadOnlyList<SeriesTreeItem<ConeBeamCtImage>> coneBeamImageSeries, IReadOnlyList<RegistrationTreeItem> registrations)
            : base(plan, fileName)
        {
            Beams = beams;
            StructureSet = structureSet;
            Doses = doses;
            ConeBeamImageSeries = coneBeamImageSeries;
            Registrations = registrations;
        }

        internal StructureSetTreeItem? StructureSet { get; }

        internal IReadOnlyList<DoseTreeItem> Doses { get; }

        internal IReadOnlyList<BeamTreeItem> Beams { get; }

        internal IReadOnlyList<SeriesTreeItem<ConeBeamCtImage>> ConeBeamImageSeries { get; }

        internal IReadOnlyList<RegistrationTreeItem> Registrations { get; }

        internal static PlanTreeItem Create(ReadOnlyCollectedSeries collectedSeries, InstanceAndFileName instanceAndFileName)
        {
            var rtPlanInstance = (RtPlan)instanceAndFileName.Instance;
            var fileName = instanceAndFileName.FileName;

            var beamTreeItems = FindBeamReferences(collectedSeries, rtPlanInstance);
            var structureSetTreeItem = rtPlanInstance.ReferencedStructureSet != null ? FindPlanStructureSet(collectedSeries, rtPlanInstance.ReferencedStructureSet) : null;
            var doseTreeItems = FindPlanDoses(collectedSeries, rtPlanInstance.InstanceUid);
            var coneBeamCtImageTreeItems = FindConeBeamCtImages(collectedSeries, rtPlanInstance.InstanceUid);

            var registrations = structureSetTreeItem != null
                ? FindRegisteredStructureSetsAndImages(collectedSeries, structureSetTreeItem.Instance)
                : new List<RegistrationTreeItem>();

            return new PlanTreeItem(rtPlanInstance, fileName, beamTreeItems, structureSetTreeItem, doseTreeItems, coneBeamCtImageTreeItems, registrations);
        }

        private static List<BeamTreeItem> FindBeamReferences(ReadOnlyCollectedSeries collectedSeries, RtPlan plan)
        {
            var referringRtImages = collectedSeries.FindInstances(ModalityType.RtImage,
                instance => ((RtImage)instance.Instance).ReferencedPlan?.InstanceUid == plan.InstanceUid).ToList();

            var referringTreatmentRecords = collectedSeries.FindInstances(ModalityType.TreatmentRecord,
                instance => ((TreatmentRecord)instance.Instance).ReferencedPlan.InstanceUid == plan.InstanceUid);

            var beamReferenceInstances = new List<(RtBeam Beam, List<InstanceAndFileName> DrrImages, List<InstanceAndFileName> RtImages, List<InstanceAndFileName> TreatmentRecords)>();
            foreach (var beam in plan.Beams)
            {
                var drrImages = beam.DrrImageReferences.Join(referringRtImages, drr => drr.InstanceUid, rtImage => rtImage.Instance.InstanceUid,
                    (drr, rtImage) => rtImage).ToList();

                drrImages.ForEach(i => referringRtImages.Remove(i));  // Remove DRR's

                var referringImages = referringRtImages.Where(i => ((RtImage)i.Instance).ReferencedBeamNumber == beam.BeamNumber).ToList();

                var referringBeamTreatmentRecords = referringTreatmentRecords
                    .Where(i => ((TreatmentRecord)i.Instance).ReferencedBeamNumber == beam.BeamNumber)
                    .OrderBy(i => ((TreatmentRecord)i.Instance).CurrentFractionNumber)
                    .ToList();

                beamReferenceInstances.Add((beam, drrImages, referringImages, referringBeamTreatmentRecords));
            }

            return beamReferenceInstances.Select(b =>
                BeamTreeItem.Create(b.Beam,
                    b.DrrImages.Select(i => ImageTreeItem<RtImage>.Create((RtImage)i.Instance, i.FileName)).ToList(),
                    b.RtImages.Select(i => ImageTreeItem<RtImage>.Create((RtImage)i.Instance, i.FileName)).ToList(),
                    b.TreatmentRecords.Select(i => TreatmentRecordTreeItem.Create((TreatmentRecord)i.Instance, i.FileName)).ToList())).ToList();
        }

        private static StructureSetTreeItem? FindPlanStructureSet(ReadOnlyCollectedSeries collectedSeries, InstanceReference structureSetInstanceReference)
        {
            var allStructureSetSeries = collectedSeries.Where(s
                => s.Modality.Type == ModalityType.StructureSet).ToList();
            var (structureSetSeriesUid, structureSetInstanceAndFileName) = allStructureSetSeries.Select(s =>
            {
                (DicomUID? SeriesUid, InstanceAndFileName? InstanceAndFileName) ret = (null, null);

                if (s.Instances.TryGetValue(structureSetInstanceReference.InstanceUid, out var instance))
                {
                    ret = (s.SeriesUid, instance);
                    return ret;
                }

                return ret;
            }).SingleOrDefault(s => s.SeriesUid != null);
            if (structureSetSeriesUid == null || structureSetInstanceAndFileName == null)
                return null;

            return StructureSetTreeItem.Create(collectedSeries, structureSetInstanceAndFileName);
        }

        private static List<DoseTreeItem> FindPlanDoses(ReadOnlyCollectedSeries collectedSeries, DicomUID planUid)
        {
            var doses = collectedSeries.FindInstances(ModalityType.Dose, instance => ((RtDose)instance.Instance).ReferencedPlan.InstanceUid == planUid);
            return doses.Select(d => DoseTreeItem.Create((RtDose)d.Instance, d.FileName)).ToList();
        }

        private static List<SeriesTreeItem<ConeBeamCtImage>> FindConeBeamCtImages(ReadOnlyCollectedSeries collectedSeries, DicomUID planInstanceUid)
        {
            var coneBeamSeries = collectedSeries.Where(s =>
                s.Modality.Type == ModalityType.CtImage && s.Instances.First is ConeBeamCtImage image && image.ReferencedPlan.InstanceUid == planInstanceUid).ToList();

            return coneBeamSeries.Select(s =>
            {
                var images = s.Instances.Select(i =>
                    ImageTreeItem<ConeBeamCtImage>.Create((ConeBeamCtImage)i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                return SeriesTreeItem<ConeBeamCtImage>.Create(s.SeriesUid, s.Modality, images);
            }).ToList();
        }

        private static List<RegistrationTreeItem> FindRegisteredStructureSetsAndImages(ReadOnlyCollectedSeries collectedSeries, RtStructureSet rtStructureSet)
        {
            var structureSets = collectedSeries.FindInstances(ModalityType.StructureSet, instance => instance.Instance != rtStructureSet);

            var registrations = collectedSeries.FindInstances(ModalityType.Registration, instance =>
                ((Registration)instance.Instance).FrameOfReferenceUid1 == rtStructureSet.FrameOfReferenceUid).ToList();

            var ctImageSeries = collectedSeries.Find(ModalityType.CtImage);

            var registrationTreeItems = new List<RegistrationTreeItem>();
            foreach (var registrationInstance in registrations)
            {
                var registration = (Registration)registrationInstance.Instance;

                var registeredStructureSets = structureSets.Where(s =>
                    ((RtStructureSet)s.Instance).FrameOfReferenceUid == registration.FrameOfReferenceUid2)
                    .Select(s => StructureSetTreeItem.Create(collectedSeries, s)).ToList();

                var registeredCtSeries = ctImageSeries.Where(i =>
                    ((CtImage) i.Instances.First).FrameOfReferenceUid == registration.FrameOfReferenceUid2
                    && !registeredStructureSets.Exists(r => r.Instance.FrameOfReferenceUid == registration.FrameOfReferenceUid2))
                    .Select(imageSeries =>
                    {
                        var images = imageSeries.Instances.Select(i =>
                            ImageTreeItem<Image>.Create((CtImage)i.InstanceAndFileName.Instance,
                                i.InstanceAndFileName.FileName)).ToList();
                        return SeriesTreeItem<Image>.Create(imageSeries.SeriesUid, imageSeries.Modality, images);
                    }).ToList();


                var registrationTreeItem = RegistrationTreeItem.Create(registration, registrationInstance.FileName,
                    registeredStructureSets, registeredCtSeries);
                registrationTreeItems.Add(registrationTreeItem);
            }
            return registrationTreeItems;
        }

        internal void RemoveAllReferencedInstances(CollectedSeries collectedSeries)
        {
            // Plan
            collectedSeries.FindAndRemoveInstances(ModalityType.Plan, instance => instance.Instance == Instance);
            // Beam References (RtImages and TreatmentRecords)
            foreach (var beamTreeItem in Beams)
            {
                collectedSeries.RemoveInstances(beamTreeItem.DrrImageTreeItems.Select(i => i.Instance).ToList());
                collectedSeries.RemoveInstances(beamTreeItem.RtImageTreeItems.Select(i => i.Instance).ToList());
                collectedSeries.RemoveInstances(beamTreeItem.TreatmentRecordTreeItems.Select(t => t.Instance).ToList());
            }
            // StructureSet and images
            if (StructureSet != null)
            {
                if (StructureSet.ImageSeries != null)
                    collectedSeries.Remove(StructureSet.ImageSeries.SeriesUid);
                collectedSeries.RemoveInstances(new List<Instance> {StructureSet.Instance});
            }
            // Doses
            foreach (var doseTreeItem in Doses)
                collectedSeries.RemoveInstances(new List<Instance>{doseTreeItem.Instance});
            // ConeBeamImages
            foreach (var seriesTreeItem in ConeBeamImageSeries)
                collectedSeries.Remove(seriesTreeItem.SeriesUid);
            // Registrations
            foreach (var registration in Registrations)
            {
                foreach (var registrationStructureSet in registration.StructureSets)
                {
                    if (registrationStructureSet.ImageSeries != null)
                        collectedSeries.Remove(registrationStructureSet.ImageSeries.SeriesUid);
                    collectedSeries.Remove(registrationStructureSet.Instance.SeriesUid);
                }
            }
        }
    }
}
