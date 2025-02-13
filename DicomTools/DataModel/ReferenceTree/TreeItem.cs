namespace DicomTools.DataModel.ReferenceTree
{
    internal abstract class TreeItem(string fileName)
    {
        internal string FileName { get; } = fileName;

        internal bool HasBeenSent { get; set; }
    }
}
