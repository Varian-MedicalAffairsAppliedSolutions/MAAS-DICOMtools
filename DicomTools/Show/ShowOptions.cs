using DicomTools.CommandLineExtensions;

namespace DicomTools.Show
{
    public class ShowOptions : ICommandOptions
    {
        public string Path { get; set; } = string.Empty;

        public string SearchPattern { get; set; } = string.Empty;

        public string Format { get; set; } = string.Empty;

        public string[] DefaultMachines { get; set; } = [];
    }
}
