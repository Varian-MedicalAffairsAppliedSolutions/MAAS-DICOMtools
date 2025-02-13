using System.Reflection;
using DicomTools.Configuration;
using FellowOakDicom;
using Microsoft.Extensions.Configuration;
using Anonymizer = FellowOakDicom.DicomAnonymizer;

namespace DicomTools
{
    public class DicomAnonymizer
    {
        public DicomAnonymizer(IConfiguration configuration)
        {
            var anonymizerOptions = configuration.GetRequiredSection("DicomAnonymizer").Get<DicomAnonymizerConfiguration>();
            if (anonymizerOptions == null)
                throw new ArgumentException("DicomAnonymizer settings not found.");
            var profilePath = GetProfilePath(anonymizerOptions.SecurityProfileFileName);
            using var profileReader = File.OpenText(profilePath);
            var securityProfileOptions = MergeOptions(Anonymizer.SecurityProfileOptions.BasicProfile, anonymizerOptions.SecurityProfileOptions);
            m_profile = Anonymizer.SecurityProfile.LoadProfile(profileReader, securityProfileOptions);

            m_anonymizer = new Anonymizer(m_profile);
        }

        public void AnonymizeInPlace(DicomDataset dataset, string newPatientId, string newPatientName)
        {
            m_profile.PatientID = newPatientId;
            m_profile.PatientName = newPatientName;
            m_anonymizer.AnonymizeInPlace(dataset);
        }

        private static string GetProfilePath(string fileName)
        {
            if (!Path.IsPathRooted(fileName))
            {
                var binDirectoryName = Path.GetDirectoryName(Assembly.GetAssembly(typeof(DicomAnonymizer))!.Location);
                fileName = Path.Combine(binDirectoryName!, "Configuration", fileName);
            }
            return fileName;
        }

        private static Anonymizer.SecurityProfileOptions MergeOptions(Anonymizer.SecurityProfileOptions options, string[] optionNames)
        {
            foreach (var optionName in optionNames)
                options |= (Anonymizer.SecurityProfileOptions) Enum.Parse(typeof(Anonymizer.SecurityProfileOptions), optionName);
            return options;
        }

        private readonly Anonymizer m_anonymizer;

        private readonly Anonymizer.SecurityProfile m_profile;
    }
}
