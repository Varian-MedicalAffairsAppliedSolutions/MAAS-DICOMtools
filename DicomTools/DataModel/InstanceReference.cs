using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class InstanceReference(Modality modality, DicomUID instanceUid) : IEquatable<InstanceReference>
    {
        public Modality Modality { get; } = modality;

        public DicomUID InstanceUid { get; } = instanceUid;

        public bool Equals(InstanceReference? other)
        {
            return Modality.Equals(other?.Modality) && InstanceUid.UID.Equals(other.InstanceUid.UID);
        }

        public override bool Equals(object? obj)
        {
            return obj is InstanceReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Modality, InstanceUid.UID);
        }
    }
}
