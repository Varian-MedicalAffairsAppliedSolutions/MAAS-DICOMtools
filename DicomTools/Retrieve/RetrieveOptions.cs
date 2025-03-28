using DicomTools.CommandLineExtensions;

namespace DicomTools.Retrieve
{
    public class RetrieveOptions : ICommandOptions
    {
        public string PatientId { get; set; } = string.Empty;

        public string PlanId { get; set; } = string.Empty;

        public bool OnlyApprovedPlans { get; set; }

        public string NewPatientId { get; set; } = string.Empty;

        public string NewPatientName { get; set; } = string.Empty;

        public bool Anonymize { get; set; }

        public string Path { get; set; } = string.Empty;

        public bool ShowTree { get; set; }

        public bool UseGet { get; set; }

        public string HostName { get; set; } = string.Empty;

        public int HostPort { get; set; }

        public string CallingAet { get; set; } = string.Empty;

        public string CalledAet { get; set; } = string.Empty;

        public bool UseTls { get; set; }

        public void CopyTo(RetrieveOptions other)
        {
            other.PatientId = PatientId;
            other.PlanId = PlanId;
            other.OnlyApprovedPlans = OnlyApprovedPlans;
            other.NewPatientId = NewPatientId;
            other.NewPatientName = NewPatientName;
            other.Anonymize = Anonymize;
            other.Path = Path;
            other.ShowTree = ShowTree;
            other.UseGet = UseGet;
            other.HostName = HostName;
            other.HostPort = HostPort;
            other.CallingAet = CallingAet;
            other.CalledAet = CalledAet;
            other.UseTls = UseTls;
        }
    }
}
