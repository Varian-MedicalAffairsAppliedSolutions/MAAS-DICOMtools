using DicomTools.CommandLineExtensions;
using System.CommandLine;

namespace DicomTools.Show
{
    public class ShowCommand : Command<ShowOptions, ShowCommandHandler>
    {
        public ShowCommand() : base("show", "Shows plans and related data as a tree from given path recursive.")
        {
            AddOption(new Option<string>(
                name: "--path",
                description: "Path where to search for files.")
            {
                IsRequired = true
            });
            AddOption(new Option<string>(
                name: "--searchPattern",
                description: "File search pattern, for example *.dcm.",
                getDefaultValue: () => "*.*"));
            AddOption(new Option<string>(
                name: "--format",
                description: "Either flat list or tree.",
                getDefaultValue: () => "tree")
            {
                IsRequired = true
            }.FromAmong("tree", "flat"));
            AddOption(new Option<string[]>(
                name: "--defaultMachines",
                description: "DefaultMachines, like RDS=HALCYON 23EX=D")
            {
                IsRequired = false,
                AllowMultipleArgumentsPerToken = true
            });
        }
    }
}
