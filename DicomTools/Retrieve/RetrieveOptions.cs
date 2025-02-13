using DicomTools.CommandLineExtensions;

namespace DicomTools.Retrieve
{
    public class DicomStorageRetrieveOptions : ICommandOptions
    {
        public string PatientId { get; set; } = string.Empty;

        public string PlanId { get; set; } = string.Empty;

        public bool OnlyApprovedPlans { get; set; }

        public string NewPatientId { get; set; } = string.Empty;

        public string NewPatientName { get; set; } = string.Empty;

        public bool Anonymize { get; set; }

        public string Path { get; set; } = string.Empty;

        public bool ShowTree { get; set; }

        public void CopyFrom(DicomStorageRetrieveOptions other)
        {
            PatientId = other.PatientId;
            PlanId = other.PlanId;
            OnlyApprovedPlans = other.OnlyApprovedPlans;
            NewPatientId = other.NewPatientId;
            NewPatientName = other.NewPatientName;
            Anonymize = other.Anonymize;
            Path = other.Path;
            ShowTree = other.ShowTree;
        }
    }

    public class RetrieveOptions : DicomStorageRetrieveOptions
    {
        public string HostName { get; set; } = string.Empty;

        public int HostPort { get; set; }

        public string CallingAet { get; set; } = string.Empty;

        public string CalledAet { get; set; } = string.Empty;
    }
}
