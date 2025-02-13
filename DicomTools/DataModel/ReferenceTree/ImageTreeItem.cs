namespace DicomTools.DataModel.ReferenceTree
{
    internal class ImageTreeItem<T> : InstanceTreeItem<T>
        where T : Image
    {
        private ImageTreeItem(T image, string fileName) : base(image, fileName)
        {
        }

        internal static ImageTreeItem<T> Create(T image, string fileName)
        {
            return new ImageTreeItem<T>(image, fileName);
        }
    }
}
