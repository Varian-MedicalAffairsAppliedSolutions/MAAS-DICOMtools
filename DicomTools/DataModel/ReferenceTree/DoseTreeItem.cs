namespace DicomTools.DataModel.ReferenceTree
{
    internal class DoseTreeItem : InstanceTreeItem<RtDose>
    {
        private DoseTreeItem(RtDose dose, string fileName)
            : base(dose, fileName)
        {
        }

        internal static DoseTreeItem Create(RtDose dose, string fileName)
        {
            return new DoseTreeItem(dose, fileName);
        }
    }
}
