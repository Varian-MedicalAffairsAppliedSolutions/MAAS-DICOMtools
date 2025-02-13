using FellowOakDicom.Imaging.Mathematics;

namespace CheckTiltedCT
{
    internal class DirectionVectors
    {
        internal Vector3D DirectionX { get; }

        internal Vector3D DirectionY { get; }

        internal string Orientation { get; }

        internal DirectionVectors(string orientation,
            double rowX, double rowY, double rowZ,
            double colX, double colY, double colZ)
        {
            Orientation = orientation;
            DirectionX = new Vector3D(rowX, rowY, rowZ);
            DirectionY = new Vector3D(colX, colY, colZ);
        }

        internal static DirectionVectors CreateFromPatientPosition(string orientation)
        {
            var allSupportedDirectionVectors = GetAllSupportedOrientations();
            return allSupportedDirectionVectors.Single(v => v.Orientation == orientation);
        }

        internal static DirectionVectors[] GetAllSupportedOrientations()
        {
            DirectionVectors[] allDirectionVectors =
            [
                new ("HFS", 1.0, 0.0, 0.0, 0.0, 1.0, 0.0),
                new ("HFP", -1.0, 0.0, 0.0, 0.0, -1.0, 0.0),
                new ("HFP", -1.0, 0.0, 0.0, 0.0, -1.0, 0.0),
                new ("FFS", -1.0, 0.0, 0.0, 0.0, 1.0, 0.0),
                new ("FFP", 1.0, 0.0, 0.0, 0.0, -1.0, 0.0),
                new ("HFDR", 0.0, 1.0, 0.0, -1.0, 0.0, 0.0),
                new ("HFDL", 0.0, -1.0, 0.0, 1.0, 0.0, 0.0),
                new ("FFDR", 0.0, -1.0, 0.0, -1.0, 0.0, 0.0),
                new ("FFDL", 0.0, 1.0, 0.0, 1.0, 0.0, 0.0),
                new ("FAFSIT", -1.0, 0.0, 0.0, 0.0, 0.0, -1.0)
            ];
            return allDirectionVectors;
        }
    }
}
