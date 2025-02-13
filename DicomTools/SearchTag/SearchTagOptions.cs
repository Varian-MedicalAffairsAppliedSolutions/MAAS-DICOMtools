using DicomTools.CommandLineExtensions;

namespace DicomTools.SearchTag
{
    public class SearchTagOptions : ICommandOptions
    {
        public string[] Tag { get; set; } = [];

        public string Path { get; set; } = string.Empty;

        public string SearchPattern { get; set; } = string.Empty;

        public bool ShowStatistics { get; set; }

        public bool ShowOnlyDirectories { get; set; }
    }
}
