using DicomTools.CommandLineExtensions;

namespace DicomTools.Store
{
    public class StoreCommand : Command<StoreOptions, StoreCommandHandler>
    {
        public StoreCommand(StoreOptions? storeOptions) : base("store", "Store patient data using C-STORE.")
        {
            AddOption("--path", "Path where to search for files to send to SCP.", isRequired: true, storeOptions?.Path);
            AddOption("--searchPattern", "File search pattern, for example *.dcm.", isRequired: false, storeOptions?.SearchPattern ?? "*.*");
            AddOption("--showMessages", "Controls whether status messages are shown or not.", isRequired: false, storeOptions?.ShowOptions);
            AddOption("--statusFileName", "Name of the file containing list of files stored. Storing of listed files will be skipped.", isRequired: false, storeOptions?.StatusFileName);

            var machineMappingOption = AddOption("--machineMapping", "MachineMapping, like A=B C=D", isRequired: false, storeOptions?.MachineMapping);
            machineMappingOption.AllowMultipleArgumentsPerToken = true;

            var defaultMachinesOption = AddOption("--defaultMachines", "DefaultMachines, like RDS=HALCYON 23EX=D", isRequired: false, storeOptions?.DefaultMachines);
            defaultMachinesOption.AllowMultipleArgumentsPerToken = true;

            AddOption("--hostName", "Name of the SCP host.", isRequired: true, storeOptions?.HostName);
            AddOption("--hostPort", "Port number of the SCP configuration.", isRequired: true, storeOptions?.HostPort);
            AddOption("--callingAet", "AET of the sender.", isRequired: true, storeOptions?.CallingAet);
            AddOption("--calledAet", "AET of the SCP.", isRequired: true, storeOptions?.CalledAet);
        }
    }
}
