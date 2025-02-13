namespace DicomTools.DataModel.ReferenceTree
{
    internal class InstanceTreeItem<T> : TreeItem where T : Instance
    {
        internal T Instance { get; }

        protected InstanceTreeItem(T instance, string fileName)
            : base(fileName)
        {
            Instance = instance;
        }
    }
}
