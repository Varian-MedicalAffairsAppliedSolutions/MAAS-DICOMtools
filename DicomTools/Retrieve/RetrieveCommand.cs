using System.CommandLine;
using DicomTools.CommandLineExtensions;

namespace DicomTools.Retrieve
{
    public class RetrieveCommand : Command<RetrieveOptions, RetrieveCommandHandler>
    {
        public RetrieveCommand(RetrieveOptions? retrieveOptions) : base("retrieve", "Retrieve patient data using Q/R.")
        {
            AddOption("--patientId", "Id of the patient to retrieve data.", isRequired: true, retrieveOptions?.PatientId);
            AddOption("--planId", "Id of the plan to retrieve data.", isRequired: false,retrieveOptions?.PlanId);
            AddOption("--onlyApprovedPlans", "Retrieve only approved plans.", isRequired: false, retrieveOptions?.OnlyApprovedPlans);
            AddOption("--newPatientId", "New patient id for the saved data.", isRequired: false, retrieveOptions?.NewPatientId);
            AddOption("--newPatientName", "New patient name for the saved data.", isRequired: false, retrieveOptions?.NewPatientName);
            AddOption("--anonymize", "Anonymize all the saved data.", isRequired: false, retrieveOptions?.Anonymize);
            AddOption("--path", "Path where to export files.", isRequired: true, retrieveOptions?.Path);
            AddOption("--showTree", "Shows the retrieved data as a tree.", isRequired: false, retrieveOptions?.ShowTree);
            AddOption("--hostName", "Name of the Dicom Service host.", isRequired: true, retrieveOptions?.HostName);
            AddOption("--hostPort", "Port number of the Dicom Services configuration.", isRequired: true, retrieveOptions?.HostPort);
            AddOption("--callingAet", "AET of the sender.", isRequired: true, retrieveOptions?.CallingAet);
            AddOption("--calledAet", "AET of the Dicom Services.", isRequired: true, retrieveOptions?.CalledAet);
            AddOption("--useGet", "Use C-GET instead of C-MOVE.", isRequired: false, retrieveOptions?.UseGet);
        }
    }
}
