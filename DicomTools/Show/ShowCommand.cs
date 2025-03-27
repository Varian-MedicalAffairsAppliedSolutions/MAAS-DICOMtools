using DicomTools.CommandLineExtensions;
using System.CommandLine;

namespace DicomTools.Show
{
    public class ShowCommand : Command<ShowOptions, ShowCommandHandler>
    {
        public ShowCommand(ShowOptions? showOptions) : base("show", "Shows plans and related data as a tree from given path recursive.")
        {
            AddOption("--path", "Path where to search for files.", isRequired: true, showOptions?.Path);
            AddOption("--searchPattern", "File search pattern, for example *.dcm.", isRequired: false, showOptions?.SearchPattern ?? "*.*");
            var formatOption = AddOption("--format", "Either flat list or tree.", isRequired: false, showOptions?.Format ?? "tree");
            formatOption.FromAmong("tree", "flat");
            var defaultMachinesOption = AddOption("--defaultMachines", "DefaultMachines, like RDS=HALCYON 23EX=D", isRequired: false, showOptions?.DefaultMachines);
            defaultMachinesOption.AllowMultipleArgumentsPerToken = true;
        }
    }
}
