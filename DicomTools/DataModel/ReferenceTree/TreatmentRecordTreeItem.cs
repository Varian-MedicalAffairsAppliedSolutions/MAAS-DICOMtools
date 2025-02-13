namespace DicomTools.DataModel.ReferenceTree
{
    internal class TreatmentRecordTreeItem : InstanceTreeItem<TreatmentRecord>
    {
        private TreatmentRecordTreeItem(TreatmentRecord treatmentRecord, string fileName)
        : base(treatmentRecord, fileName)
        {

        }

        internal static TreatmentRecordTreeItem Create(TreatmentRecord treatmentRecord, string fileName)
        {
            return new TreatmentRecordTreeItem(treatmentRecord, fileName);
        }
    }
}
