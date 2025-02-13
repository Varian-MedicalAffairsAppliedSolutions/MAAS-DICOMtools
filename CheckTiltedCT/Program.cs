using System.Globalization;
using FellowOakDicom;

namespace CheckTiltedCT
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
                throw new ArgumentException("DICOM file name needs to be given as parameter.");

            var dicomFile = DicomFile.Open(args[0]);
            var dataset = dicomFile.Dataset;
            var sopClassUid = dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID);
            Console.WriteLine($"SOPClassUid: {sopClassUid}");

            var patientPosition = dataset.GetSingleValue<string>(DicomTag.PatientPosition);
            Console.WriteLine($"Patient position: {patientPosition}");

            var imageOrientation = dataset.GetValues<string>(DicomTag.ImageOrientationPatient);
            Console.WriteLine($"Image orientation: {string.Join('/', imageOrientation)}");

            var patientOrientation = new PatientOrientation(patientPosition, GetImageOrientationAsDoubles(imageOrientation));

            var isTilted = patientOrientation.IsTilted;
            Console.WriteLine(isTilted.Value ? $"Is tilted, {isTilted.Reason}" : "Is not tilted");

            return 0;
        }

        private static double[] GetImageOrientationAsDoubles(string[] imageOrientation)
        {
            return imageOrientation.Select(i => double.Parse(i, CultureInfo.InvariantCulture)).ToArray();
        }
    }
}
