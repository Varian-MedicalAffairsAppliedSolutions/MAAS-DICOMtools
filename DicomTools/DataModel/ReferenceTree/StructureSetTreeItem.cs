using DicomTools.DataModel.CollectFiles;

namespace DicomTools.DataModel.ReferenceTree
{
    internal class StructureSetTreeItem : InstanceTreeItem<RtStructureSet>
    {
        private StructureSetTreeItem(RtStructureSet structureSet, string fileName, SeriesTreeItem<Image>? imageSeries)
            : base(structureSet, fileName)
        {
            ImageSeries = imageSeries;
        }

        internal SeriesTreeItem<Image>? ImageSeries { get; }

        internal static StructureSetTreeItem Create(ReadOnlyCollectedSeries collectedSeries, InstanceAndFileName instanceAndFileName)
        {
            var rtStructureSet = (RtStructureSet)instanceAndFileName.Instance;

            var imageSeries = FindStructureSetImageSeries(collectedSeries, rtStructureSet);
            return new StructureSetTreeItem(rtStructureSet, instanceAndFileName.FileName, imageSeries);
        }

        private static SeriesTreeItem<Image>? FindStructureSetImageSeries(ReadOnlyCollectedSeries collectedSeries, RtStructureSet rtStructureSet)
        {
            // CT?
            var ctImageSeries = collectedSeries
                .Where(i => i.Modality.Type == ModalityType.CtImage).ToList();

            foreach (var imageSeries in ctImageSeries)
            {
                var image = (CtImage)imageSeries.Instances.First;
                if (image.FrameOfReferenceUid == rtStructureSet.FrameOfReferenceUid)
                {
                    var imageTreeItems = imageSeries.Instances.Select(i =>
                        ImageTreeItem<Image>.Create((CtImage) i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                    return SeriesTreeItem<Image>.Create(imageSeries.SeriesUid, imageSeries.Modality, imageTreeItems);
                }
            }

            // MR?
            var mrImageSeries = collectedSeries
                .Where(i => i.Modality.Type == ModalityType.MrImage).ToList();

            foreach (var imageSeries in mrImageSeries)
            {
                var image = (MrImage)imageSeries.Instances.First;
                if (image.FrameOfReferenceUid == rtStructureSet.FrameOfReferenceUid)
                {
                    var imageTreeItems = imageSeries.Instances
                        .Select(i =>
                            ImageTreeItem<Image>.Create((MrImage)i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                    return SeriesTreeItem<Image>.Create(imageSeries.SeriesUid, imageSeries.Modality, imageTreeItems);
                }
            }

            // PET?
            var petImageSeries = collectedSeries
                .Where(i => i.Modality.Type == ModalityType.PetImage).ToList();

            foreach (var imageSeries in petImageSeries)
            {
                var image = (PetImage)imageSeries.Instances.First;
                if (image.FrameOfReferenceUid == rtStructureSet.FrameOfReferenceUid)
                {
                    var imageTreeItems = imageSeries.Instances
                        .Select(i =>
                            ImageTreeItem<Image>.Create((PetImage)i.InstanceAndFileName.Instance, i.InstanceAndFileName.FileName)).ToList();
                    return SeriesTreeItem<Image>.Create(imageSeries.SeriesUid, imageSeries.Modality, imageTreeItems);
                }
            }

            return null; // Image series not found.
        }
    }
}
