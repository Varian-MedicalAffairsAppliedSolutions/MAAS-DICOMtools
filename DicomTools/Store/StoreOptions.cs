using DicomTools.CommandLineExtensions;

namespace DicomTools.Store
{
    public class StoreOptions : ICommandOptions
    {
        public string Path { get; set; } = string.Empty;

        public string SearchPattern { get; set; } = string.Empty;

        public bool ShowOptions { get; set; }

        public string HostName { get; set; } = string.Empty;

        public int HostPort { get; set; }

        public string CallingAet { get; set; } = string.Empty;

        public string CalledAet { get; set; } = string.Empty;

        public string StatusFileName { get; set; } = string.Empty;

        public string[] MachineMapping { get; set; } = [];

        public string[] DefaultMachines { get; set; } = [];
    }
}
