using DicomTools.CommandLineExtensions;
using System.CommandLine;

namespace DicomTools.Store
{
    public class StoreCommand : Command<StoreOptions, StoreCommandHandler>
    {
        public StoreCommand() : base("store", "Store patient data using C-STORE.")
        {
            AddOption(new Option<string>(
                name: "--path",
                description: "Path where to search for files to send to SCP.")
            {
                IsRequired = true
            });
            AddOption(new Option<string>(
                name: "--searchPattern",
                description: "File search pattern, for example *.dcm.",
                getDefaultValue: () => "*.*"));
            AddOption(new Option<bool>(
                name: "--showMessages",
                description: "Controls whether status messages are shown or not.",
                getDefaultValue: () => true)
            {
                IsRequired = false
            });
            AddOption(new Option<string>(
                name: "--statusFileName",
                description: "Name of the file containing list of files stored. Storing of listed files will be skipped."));
            AddOption(new Option<string[]>(
                name: "--machineMapping",
                description: "MachineMapping, like A=B C=D")
            {
                IsRequired = false,
                AllowMultipleArgumentsPerToken = true
            });
            AddOption(new Option<string[]>(
                name: "--defaultMachines",
                description: "DefaultMachines, like RDS=HALCYON 23EX=D")
            {
                IsRequired = false,
                AllowMultipleArgumentsPerToken = true
            });
            AddOption(new Option<string>(
                name: "--hostName",
                description: "Name of the SCP host.")
            {
                IsRequired = true
            });
            AddOption(new Option<int>(
                name: "--hostPort",
                description: "Port number of the SCP configuration.")
            {
                IsRequired = true
            });
            AddOption(new Option<string>(
                name: "--callingAet",
                description: "AET of the sender.")
            {
                IsRequired = true
            });
            AddOption(new Option<string>(
                name: "--calledAet",
                description: "AET of the SCP.")
            {
                IsRequired = true
            });
        }
    }
}
