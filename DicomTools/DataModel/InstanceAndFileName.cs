namespace DicomTools.DataModel
{
    internal class InstanceAndFileName
    {
        internal Instance Instance { get; }

        internal string FileName { get; }

        internal InstanceAndFileName(Instance instance, string fileName)
        {
            Instance = instance;
            FileName = fileName;
        }
    }
}
