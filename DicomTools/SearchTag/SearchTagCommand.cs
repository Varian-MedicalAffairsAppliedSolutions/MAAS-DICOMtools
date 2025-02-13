using DicomTools.CommandLineExtensions;
using System.CommandLine;

namespace DicomTools.SearchTag
{
    public class SearchTagCommand : Command<SearchTagOptions, SearchTagCommandHandler>
    {
        public SearchTagCommand() : base("search", "Search dicom tag values from given path recursive.\n" +
            "Examples:\n" +
            "List all where PatientId is Phantom-1 and show modality:\n" +
            "  --tag \"(0010,0020)=Phantom-1\" --tag \"(0008,0060)=?\" --path X:\\Data --searchPattern *.dcm --showStatistics\n" +
            "List all treatment unit names:\n" +
            "  --tag \"(300A,00B0)/(300A,00B2)=?\" --path X:\\Data --searchPattern RP*.dcm --showStatistics")
        {
            AddOption(new Option<string[]>(
                name: "--tag",
                description: "Dicom tags to search in a format (gggg,eee)=value.\nFor example --tag (0010,0020)=PatientId --tag (0008,0060)=CT.")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore
            });
            AddOption(new Option<string>(
                name: "--path",
                description: "Path where to search for files including given tag.")
            {
                IsRequired = true
            });
            AddOption(new Option<string>(
                name: "--searchPattern",
                description: "File search pattern, for example *.dcm.",
                getDefaultValue: () => "*.*"));
            AddOption(new Option<bool>(
                name: "--showStatistics",
                description: "Show statistics")
            {
                Arity = ArgumentArity.Zero
            });
            AddOption(new Option<bool>(
                name: "--showOnlyDirectories",
                description: "List only directories, not files.")
            {
                Arity = ArgumentArity.Zero,
            });
        }
    }
}
