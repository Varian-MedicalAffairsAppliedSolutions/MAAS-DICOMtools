using DicomTools.DataModel.CollectFiles;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DicomTools.DataModel.ReferenceTree
{
    internal class ReferenceTree
    {
        internal IReadOnlyList<PlanTreeItem> Plans { get; }

        // Rest not connected to a plan.
        internal IReadOnlyList<StructureSetTreeItem> StructureSets { get; }

        internal IReadOnlyList<DoseTreeItem> Doses { get; }

        internal IReadOnlyList<RegistrationTreeItem> Registrations { get; }

        internal IReadOnlyList<SeriesTreeItem<ConeBeamCtImage>> ConeBeamImages { get; }

        internal IReadOnlyList<SeriesTreeItem<CtImage>> CtImages { get; }

        internal IReadOnlyList<SeriesTreeItem<PetImage>> PetImages { get; }

        internal IReadOnlyList<SeriesTreeItem<MrImage>> MrImages { get; }

        internal IReadOnlyList<SeriesTreeItem<RtImage>> RtImages { get; }

        private ReferenceTree(IReadOnlyList<PlanTreeItem> plans, IReadOnlyList<StructureSetTreeItem> structureSets, IReadOnlyList<SeriesTreeItem<RtImage>> rtImages,
            IReadOnlyList<SeriesTreeItem<ConeBeamCtImage>> coneBeamImages, IReadOnlyList<DoseTreeItem> doses, IReadOnlyList<SeriesTreeItem<CtImage>> ctImages,
            IReadOnlyList<SeriesTreeItem<MrImage>> mrImages, IReadOnlyList<SeriesTreeItem<PetImage>> petImages, IReadOnlyList<RegistrationTreeItem> registrations)
        {
            Plans = plans;
            StructureSets = structureSets;
            RtImages = rtImages;
            ConeBeamImages = coneBeamImages;
            Doses = doses;
            CtImages = ctImages;
            MrImages = mrImages;
            PetImages = petImages;
            Registrations = registrations;
        }

        internal static ReferenceTree Create(ILogger logger, IConsole console, CollectedSeries collectedSeries)
        {
            var planTreeItems = CreatePlanTreeItems(collectedSeries);

            // Unconnected (remove references from plan after this)
            var structureSets = FindAndRemoveUnconnectedStructureSets(collectedSeries);
            
            planTreeItems.ForEach(p => p.RemoveAllReferencedInstances(collectedSeries));

            var coneBeamImages = FindAndRemoveUnconnectedConeBeamImages(collectedSeries);
            var rtImages = FindAndRemoveUnconnectedRtImages(collectedSeries);

            var doses = FindAndRemoveUnconnectedDoses(collectedSeries);
            var ctImages = FindAndRemoveUnconnectedCtImages(collectedSeries);
            var mrImages = FindAndRemoveUnconnectedMrImages(collectedSeries);
            var petImages = FindAndRemoveUnconnectedPetImages(collectedSeries);
            var registrations = FindAndRemoveUnconnectedRegistrations(collectedSeries);

            return new ReferenceTree(planTreeItems, structureSets, rtImages, coneBeamImages, doses, ctImages, mrImages, petImages, registrations);
        }

        private static List<PlanTreeItem> CreatePlanTreeItems(CollectedSeries collectedSeries)
        {
            var planTreeItems = new List<PlanTreeItem>();

            var planSeries = collectedSeries.Where(i 
                => i.Modality.Type == ModalityType.Plan).ToList();

            foreach (var planSeriesInstances in planSeries)
            {
                foreach (var planInstance in planSeriesInstances.Instances)
                {
                    var planTreeItem = PlanTreeItem.Create(collectedSeries, planInstance.InstanceAndFileName);
                    planTreeItems.Add(planTreeItem);
                }
                // Remove plan.
                collectedSeries.Remove(planSeriesInstances.SeriesUid);
            }

            // Remove referenced structure sets.
            foreach (var planTreeItem in planTreeItems)
            {
                if (planTreeItem.StructureSet != null)
                {
                    collectedSeries.Remove(planTreeItem.StructureSet.Instance.SeriesUid);
                    foreach (var registration in planTreeItem.Registrations)
                    {
                        foreach (var structureSet in registration.StructureSets)
                        {
                            collectedSeries.Remove(structureSet.Instance.SeriesUid);
                        }
                    }
                }
            }

            return planTreeItems;
        }


        private static List<StructureSetTreeItem> FindAndRemoveUnconnectedStructureSets(CollectedSeries collectedSeries)
        {
            var instances = collectedSeries.FindAndRemoveInstances(ModalityType.StructureSet, _ => true);
            return instances.Select(s => StructureSetTreeItem.Create(collectedSeries, s)).ToList();
        }

        private static List<SeriesTreeItem<RtImage>> FindAndRemoveUnconnectedRtImages(CollectedSeries collectedSeries)
        {
            var rtImageSeries = collectedSeries.FindAndRemove(ModalityType.RtImage);
            return rtImageSeries.Select(s =>
            {
                var imageTreeItems = s.Instances.Select(i => 
                    ImageTreeItem<RtImage>.Create((RtImage)i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                return SeriesTreeItem<RtImage>.Create(s.SeriesUid, s.Modality, imageTreeItems);
            }).ToList();
        }

        private static List<DoseTreeItem> FindAndRemoveUnconnectedDoses(CollectedSeries collectedSeries)
        {
            // Not connected to any plan.
            var instances = collectedSeries.FindAndRemoveInstances(ModalityType.Dose, _ => true);
            return instances.Select(d => DoseTreeItem.Create((RtDose) d.Instance, d.FileName)).ToList();
        }

        private static List<SeriesTreeItem<ConeBeamCtImage>> FindAndRemoveUnconnectedConeBeamImages(CollectedSeries collectedSeries)
        {
            var series = collectedSeries
                .Where(s => s.Modality.Type == ModalityType.CtImage && s.Instances.First is ConeBeamCtImage).ToList();

            series.ForEach(s => collectedSeries.Remove(s.SeriesUid));

            return series.Select(s =>
            {
                var imageTreeItems = s.Instances.Select(i =>
                    ImageTreeItem<ConeBeamCtImage>.Create((ConeBeamCtImage) i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                return SeriesTreeItem<ConeBeamCtImage>.Create(s.SeriesUid, s.Modality, imageTreeItems);
            }).ToList();
        }

        private static List<SeriesTreeItem<CtImage>> FindAndRemoveUnconnectedCtImages(CollectedSeries collectedSeries)
        {
            var instances = collectedSeries.FindAndRemove(ModalityType.CtImage);
            return instances.Select(s =>
            {
                var imageTreeItems = s.Instances.Select(i =>
                    ImageTreeItem<CtImage>.Create((CtImage)i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                return SeriesTreeItem<CtImage>.Create(s.SeriesUid, s.Modality, imageTreeItems);
            }).ToList();
        }

        private static List<SeriesTreeItem<MrImage>> FindAndRemoveUnconnectedMrImages(CollectedSeries collectedSeries)
        {
            var instances = collectedSeries.FindAndRemove(ModalityType.MrImage);
            return instances.Select(s =>
            {
                var imageTreeItems = s.Instances.Select(i =>
                    ImageTreeItem<MrImage>.Create((MrImage)i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                return SeriesTreeItem<MrImage>.Create(s.SeriesUid, s.Modality, imageTreeItems);
            }).ToList();
        }

        private static List<SeriesTreeItem<PetImage>> FindAndRemoveUnconnectedPetImages(CollectedSeries collectedSeries)
        {
            var instances = collectedSeries.FindAndRemove(ModalityType.PetImage);
            return instances.Select(s =>
            {
                var imageTreeItems = s.Instances.Select(i =>
                    ImageTreeItem<PetImage>.Create((PetImage)i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                return SeriesTreeItem<PetImage>.Create(s.SeriesUid, s.Modality, imageTreeItems);
            }).ToList();
        }

        private static List<RegistrationTreeItem> FindAndRemoveUnconnectedRegistrations(CollectedSeries collectedSeries)
        {
            var registrations = collectedSeries.FindAndRemoveInstances(ModalityType.Registration, _ => true);
            return registrations.Select(r =>
                RegistrationTreeItem.Create((Registration)r.Instance, r.FileName,
                    new List<StructureSetTreeItem>(), new List<SeriesTreeItem<Image>>())
            ).ToList();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach (var planTreeItem in Plans)
            {
                stringBuilder.AppendLine($"  RT-Plan: {planTreeItem.Instance.Label}, Intent: {planTreeItem.Instance.PlanIntent}");
                if (planTreeItem.StructureSet != null)
                {
                    var structureSet = planTreeItem.StructureSet.Instance;
                    stringBuilder.AppendLine($"    RT-Struct: {structureSet.Label}");
                    var firstImage = planTreeItem.StructureSet.ImageSeries?.Instances.FirstOrDefault();
                    var modality = "Unknown";
                    var seriesUid = "Unknown";
                    if (firstImage != null)
                    {
                        modality = firstImage.Instance.Modality.ToString();
                        seriesUid = firstImage.Instance.SeriesUid.UID;
                    }

                    stringBuilder.AppendLine($"      Image {modality}: {planTreeItem.StructureSet.ImageSeries?.Instances.Count ?? 0} images (SeriesUID: {seriesUid})");
                }

                foreach (var beamTreeItem in planTreeItem.Beams)
                {
                    stringBuilder.AppendLine($"    Beam: {beamTreeItem.Beam.Label}, {beamTreeItem.Beam.TreatmentDeliveryType}");
                    foreach (var drrImageTreeItem in beamTreeItem.DrrImageTreeItems)
                    {
                        var imageType = string.Join(", ", drrImageTreeItem.Instance.ImageType);
                        stringBuilder.AppendLine($"      RT-Image (Live, {imageType}): {drrImageTreeItem.Instance.Label}, InstanceUID: {drrImageTreeItem.Instance.InstanceUid.UID}");
                    }
                    foreach (var rtImageTreeItem in beamTreeItem.RtImageTreeItems)
                    {
                        var imageType = string.Join(", ", rtImageTreeItem.Instance.ImageType);
                        stringBuilder.AppendLine($"      RT-Image (Static, {imageType}): {rtImageTreeItem.Instance.Label}, InstanceUID: {rtImageTreeItem.Instance.InstanceUid.UID}");
                    }

                    foreach (var treatmentRecordTreeItem in beamTreeItem.TreatmentRecordTreeItems)
                    {
                        stringBuilder.AppendLine($"      RT-RECORD: {treatmentRecordTreeItem.Instance.Label}");
                    }
                }

                foreach (var doseTreeItem in planTreeItem.Doses)
                    stringBuilder.AppendLine($"    RT-Dose: {doseTreeItem.Instance.Label}, Summation type: {doseTreeItem.Instance.SummationType}");
                if (planTreeItem.Registrations.Count > 0)
                    stringBuilder.AppendLine("    Registrations");
                foreach (var registration in planTreeItem.Registrations)
                {
                    stringBuilder.AppendLine($"      Registration: {registration.Instance.Label} (SeriesUID: {registration.Instance.SeriesUid.UID}");
                    foreach (var registrationStructureSet in registration.StructureSets)
                    {
                        stringBuilder.Append($"        RT-Struct: {registrationStructureSet.Instance.Label} (SeriesUID: {registrationStructureSet.Instance.SeriesUid.UID})");
                        var firstImage = registrationStructureSet.ImageSeries?.Instances.FirstOrDefault();
                        var modality = "Unknown";
                        var seriesUid = "Unknown";
                        if (firstImage != null)
                        {
                            modality = firstImage.Instance.Modality.ToString();
                            seriesUid = firstImage.Instance.SeriesUid.UID;
                        }

                        stringBuilder.AppendLine(
                            $", Image {modality}: {registrationStructureSet.ImageSeries?.Instances.Count ?? 0} images (SeriesUID: {seriesUid})");
                    }

                    foreach (var registrationImageSeries in registration.ImageSeries)
                    {
                        stringBuilder.AppendLine($"         Image {registrationImageSeries.Modality}: {registrationImageSeries.Instances.Count} images (SeriesUID: {registrationImageSeries.SeriesUid.UID})");
                    }
                }
                if (planTreeItem.ConeBeamImageSeries.Count > 0)
                    stringBuilder.AppendLine($"    CBCT: {planTreeItem.ConeBeamImageSeries.Count} images.");

                stringBuilder.AppendLine();
            }

            var hasUnconnected = StructureSets.Count > 0 || Doses.Count > 0 || Registrations.Count > 0 ||
                                 RtImages.Count > 0 || ConeBeamImages.Count > 0 ||
                                 CtImages.Count > 0 || MrImages.Count > 0 || PetImages.Count > 0;

            if (hasUnconnected)
                stringBuilder.AppendLine("Not connected to any plan");

            foreach (var structureSetTreeItem in StructureSets)
            {
                var structureSet = structureSetTreeItem.Instance;
                stringBuilder.Append($"  RT-Struct: {structureSet.Label}");
                var firstImage = structureSetTreeItem.ImageSeries?.Instances.FirstOrDefault();
                var modality = "Unknown";
                var seriesUid = "Unknown";
                if (firstImage != null)
                {
                    modality = firstImage.Instance.Modality.ToString();
                    seriesUid = firstImage.Instance.SeriesUid.UID;
                }

                stringBuilder.AppendLine($", Image {modality}: {structureSetTreeItem.ImageSeries?.Instances.Count ?? 0} images (SeriesUID: {seriesUid})");
            }

            foreach (var doseTreeItem in Doses)
                stringBuilder.AppendLine($"  RT-DOSE: {doseTreeItem.Instance.Label}");

            foreach (var registrationTreeItem in Registrations)
                stringBuilder.AppendLine($"  REG: {registrationTreeItem.Instance.Label}");

            foreach (var imageTreeItem in RtImages)
            {
                stringBuilder.AppendLine($"  RT-IMAGE: {imageTreeItem.Instances.Count} images, Series: {imageTreeItem.SeriesUid.UID}");
                foreach (var image in imageTreeItem.Instances)
                {
                    var imageType = string.Join(", ", image.Instance.ImageType);
                    stringBuilder.AppendLine($"    RT-IMAGE ({imageType}):, InstanceUID: {imageTreeItem.SeriesUid.UID}");
                }
            }

            foreach (var coneBeamTreeItem in ConeBeamImages)
            {
                stringBuilder.AppendLine($"  CBCT: {coneBeamTreeItem.Instances.Count} images, Series: {coneBeamTreeItem.SeriesUid.UID}");
            }

            foreach (var ctSeries in CtImages)
                stringBuilder.AppendLine($"  CT: {ctSeries.Instances.Count} images, Series: {ctSeries.SeriesUid.UID}");

            foreach (var mrSeries in MrImages)
                stringBuilder.AppendLine($"  MR: {mrSeries.Instances.Count} images, Series: {mrSeries.SeriesUid.UID}");

            foreach (var petSeries in PetImages)
                stringBuilder.AppendLine($"  PET: {petSeries.Instances.Count} images, Series: {petSeries.SeriesUid.UID}");

            return stringBuilder.ToString();
        }
    }
}