using DicomTools.CommandLineExtensions;
using System.CommandLine;

namespace DicomTools.SearchTag
{
    public class SearchTagCommand : Command<SearchTagOptions, SearchTagCommandHandler>
    {
        public SearchTagCommand(SearchTagOptions? searchTagOptions) : base("search", "Search dicom tag values from given path recursive.\n" +
                                                                    "Examples:\n" +
                                                                    "List all where PatientId is Phantom-1 and show modality:\n" +
                                                                    "  --tag \"(0010,0020)=Phantom-1\" --tag \"(0008,0060)=?\" --path X:\\Data --searchPattern *.dcm --showStatistics\n" +
                                                                    "List all treatment unit names:\n" +
                                                                    "  --tag \"(300A,00B0)/(300A,00B2)=?\" --path X:\\Data --searchPattern RP*.dcm --showStatistics")
        {
            var tagOption = AddOption("--tag", "Dicom tags to search in a format (gggg,eee)=value.\nFor example --tag (0010,0020)=PatientId --tag (0008,0060)=CT.",
                isRequired: true, searchTagOptions?.Tag);
            tagOption.Arity = ArgumentArity.OneOrMore;

            AddOption("--path", "Path where to search for files including given tag.", isRequired: true,searchTagOptions?.Path);
            AddOption("--searchPattern", "File search pattern, for example *.dcm.", isRequired: false, 
                searchTagOptions?.SearchPattern ?? "*.*");
            var showStatisticsOption = AddOption("--showStatistics", "Show statistics", isRequired: false, searchTagOptions?.ShowStatistics);
            showStatisticsOption.Arity = ArgumentArity.Zero;

            var showOnlyDirectoriesOption = AddOption("--showOnlyDirectories", "List only directories, not files.", isRequired: false,
                searchTagOptions?.ShowOnlyDirectories);
            showOnlyDirectoriesOption.Arity = ArgumentArity.Zero;
        }
    }
}
