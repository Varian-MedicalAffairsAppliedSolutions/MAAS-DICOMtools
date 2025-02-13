namespace DicomTools.Configuration
{
    public class DicomAnonymizerConfiguration
    {
        public string SecurityProfileFileName { get; set; } = string.Empty;

        public string[] SecurityProfileOptions { get; set; } = [];
    }
}
