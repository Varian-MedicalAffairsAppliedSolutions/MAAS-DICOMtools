using System.CommandLine;
using DicomTools.CommandLineExtensions;

namespace DicomTools.Retrieve
{
    public class RetrieveCommand : Command<RetrieveOptions, RetrieveCommandHandler>
    {
        public RetrieveCommand() : base("retrieve", "Retrieve patient data using Q/R.")
        {
            AddOption(new Option<string>(
                name: "--patientId",
                description: "Id of the patient to retrieve data.")
            {
                IsRequired = true
            });
            AddOption(new Option<string>(
                name: "--planId",
                description: "Id of the plan to retrieve data.")
            {
                IsRequired = false
            });
            AddOption(new Option<bool>(
                name: "--onlyApprovedPlans",
                description: "Retrieve only approved plans.")
            {
                IsRequired = false
            });
            AddOption(new Option<string>(
                name: "--newPatientId",
                description: "New patient id for the saved data.")
            {
                IsRequired = false
            });
            AddOption(new Option<string>(
                name: "--newPatientName",
                description: "New patient name for the saved data."));
            AddOption(new Option<bool>(
                name: "--anonymize",
                description: "Anonymize all the saved data."));

            AddOption(new Option<string>(
                name: "--path",
                description: "Path where to export files.")
            {
                IsRequired = true
            });

            AddOption(new Option<bool>(
                name: "--showTree",
                description: "Shows the retrieved data as a tree."));

            AddOption(new Option<string>(
                name: "--hostName",
                description: "Name of the Dicom Service host.")
            {
                IsRequired = true
            });

            AddOption(new Option<int>(
                name: "--hostPort",
                description: "Port number of the Dicom Services configuration.")
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
                description: "AET of the Dicom Services.")
            {
                IsRequired = true
            });
        }
    }
}
