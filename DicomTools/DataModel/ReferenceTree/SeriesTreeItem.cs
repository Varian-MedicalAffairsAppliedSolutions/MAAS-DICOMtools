using FellowOakDicom;

namespace DicomTools.DataModel.ReferenceTree
{
    internal class SeriesTreeItem<T>
        where T : Image
    {
        internal DicomUID SeriesUid { get; }

        internal Modality Modality { get; }

        internal IReadOnlyList<InstanceTreeItem<T>> Instances { get; }

        private SeriesTreeItem(DicomUID seriesUid, Modality modality, IReadOnlyList<InstanceTreeItem<T>> instances)
        {
            SeriesUid = seriesUid;
            Modality = modality;
            Instances = instances;
        }

        internal static SeriesTreeItem<T> Create(DicomUID seriesUid, Modality modality, IReadOnlyList<ImageTreeItem<T>> imageTreeItems)
        {
            return new SeriesTreeItem<T>(seriesUid, modality, imageTreeItems);
        }
    }
}
