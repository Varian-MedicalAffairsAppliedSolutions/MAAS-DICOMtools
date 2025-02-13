namespace DicomTools.DataModel.ReferenceTree
{
    internal class BeamTreeItem
    {
        internal RtBeam Beam { get; }

        internal IReadOnlyList<ImageTreeItem<RtImage>> DrrImageTreeItems { get; }

        internal IReadOnlyList<ImageTreeItem<RtImage>> RtImageTreeItems { get; }

        internal IReadOnlyList<TreatmentRecordTreeItem> TreatmentRecordTreeItems { get; }

        private BeamTreeItem(RtBeam beam, IReadOnlyList<ImageTreeItem<RtImage>> drrImageTreeItems, IReadOnlyList<ImageTreeItem<RtImage>> rtImageTreeItems,
            IReadOnlyList<TreatmentRecordTreeItem> treatmentRecordTreeItems)
        {
            Beam = beam;
            DrrImageTreeItems = drrImageTreeItems;
            RtImageTreeItems = rtImageTreeItems;
            TreatmentRecordTreeItems = treatmentRecordTreeItems;
        }

        internal static BeamTreeItem Create(RtBeam beam, IReadOnlyList<ImageTreeItem<RtImage>> drrImageTreeItems,
            IReadOnlyList<ImageTreeItem<RtImage>> rtImageTreeItems,
            IReadOnlyList<TreatmentRecordTreeItem> treatmentRecordTreeItems)
        {
            return new BeamTreeItem(beam, drrImageTreeItems, rtImageTreeItems, treatmentRecordTreeItems);
        }
    }
}
