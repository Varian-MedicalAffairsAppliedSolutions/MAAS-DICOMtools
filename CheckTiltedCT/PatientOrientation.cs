using FellowOakDicom.Imaging.Mathematics;

namespace CheckTiltedCT
{
    internal class PatientOrientation : DirectionVectors
    {
        internal DirectionVectors OrientationVector { get; }

        internal PatientOrientation(string orientation,
            double rowX, double rowY, double rowZ,
            double colX, double colY, double colZ)
            : base(orientation, rowX, rowY, rowZ, colX, colY, colZ)
        {
            OrientationVector = CreateFromPatientPosition(orientation)!;
            if (OrientationVector == null)
                throw new ArgumentException($"{orientation} is not supported.", nameof(orientation));
        }

        internal PatientOrientation(string orientation, double[] vectors)
            : this(orientation,
                vectors[0], vectors[1], vectors[2],
                vectors[3], vectors[4], vectors[5])
        {
        }

        internal (bool Value, string Reason) IsTilted
        {
            get
            {
                const double pitchTiltEpsilon = 1e-4;
                var pitch = DirectionY.Z; // In practice, image orientation component 5 tests gantry pitch during CT imaging.
                if (pitch > pitchTiltEpsilon)
                    return (true, $"Pitch ({pitch} > {pitchTiltEpsilon})");

                // Gantry yaw tilt: somewhat larger epsilon than is used for the pitch tilt check.
                const double generalTiltEpsilon = 5e-3;
                return CheckGeneralTiltAllOrientations(generalTiltEpsilon);
            }
        }

        private (bool Value, string Reason) CheckGeneralTiltAllOrientations(double epsilon)
        {
            var check = CheckGeneralTiltEpsilon(DirectionX, OrientationVector.DirectionX, epsilon);
            if (check.Value)
                return (check.Value, $"XDirection ({Orientation}): {check.Reason} is tilted.");
            check = CheckGeneralTiltEpsilon(DirectionY, OrientationVector.DirectionY, epsilon);
            if (check.Value)
                return (check.Value, $"YDirection ({Orientation}): {check.Reason} is tilted.");

            return (false, string.Empty);
        }

        private static (bool Value, string Reason) CheckGeneralTiltEpsilon(Vector3D vector, Vector3D orientationVector, double epsilon)
        {
            var x = Math.Abs(vector.X - orientationVector.X);
            if (x > epsilon)
                return (true, $"X ({x:F16} > {epsilon:F16})");
            var y = Math.Abs(vector.Y - orientationVector.Y);
            if (y > epsilon)
                return (true, $"Y ({y:F16} > {epsilon:F16})");
            var z = Math.Abs(vector.Z - orientationVector.Z);
            if (z > epsilon)
                return (true, $"Z ({z:F16} > {epsilon:F16})");
            return (false, string.Empty);
        }
    }
}
