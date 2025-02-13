namespace DicomTools.DataModel.ReferenceTree
{
    internal class RegistrationTreeItem : InstanceTreeItem<Registration>
    {
        internal IReadOnlyList<StructureSetTreeItem> StructureSets { get; }

        internal IReadOnlyList<SeriesTreeItem<Image>> ImageSeries { get; }

        private RegistrationTreeItem(Registration registration, string fileName,
            IReadOnlyList<StructureSetTreeItem>  structureSetTreeItems,
            IReadOnlyList<SeriesTreeItem<Image>> imageSeriesTreeItems)
            : base(registration, fileName)
        {
            StructureSets = structureSetTreeItems;
            ImageSeries = imageSeriesTreeItems;
        }

        internal static RegistrationTreeItem Create(Registration registration, string fileName,
            IReadOnlyList<StructureSetTreeItem> structureSetTreeItems,
            IReadOnlyList<SeriesTreeItem<Image>> imageSeriesTreeItems)
        {
            return new RegistrationTreeItem(registration, fileName, structureSetTreeItems, imageSeriesTreeItems);
        }
    }
}
