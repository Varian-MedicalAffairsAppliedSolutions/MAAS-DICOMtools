using System.Collections;
using FellowOakDicom;

namespace DicomTools.DataModel.CollectFiles
{
    internal class CollectedInstances : IEnumerable<(DicomUID InstanceUID, InstanceAndFileName InstanceAndFileName)>
    {
        internal CollectedInstances(Instance instance, string fileName)
        {
            Modality = instance.Modality;

            m_instances = new Dictionary<DicomUID, InstanceAndFileName>
            {
                { instance.InstanceUid, new InstanceAndFileName(instance, fileName) }
            };
        }

        internal bool TryGetValue(DicomUID instanceUid, out InstanceAndFileName? instanceAndFileName)
        {
            return m_instances.TryGetValue(instanceUid, out instanceAndFileName);
        }

        internal void Add(Instance instance, string fileName)
        {
            if (!instance.Modality.Equals(Modality))
                throw new ArgumentException($"Series modality is {Modality}, cannot add {instance.Modality}");

            m_instances.Add(instance.InstanceUid, new InstanceAndFileName(instance, fileName));
        }

        internal Modality Modality { get; }

        internal Instance First => m_instances.Values.First().Instance;

        public IEnumerator<(DicomUID InstanceUID, InstanceAndFileName InstanceAndFileName)> GetEnumerator()
        {
            var collectedInstances = new List<(DicomUID, InstanceAndFileName)>();
            foreach (var collectedInstance in m_instances)
            {
                collectedInstances.Add((collectedInstance.Key, collectedInstance.Value));
            }
            return collectedInstances.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_instances.GetEnumerator();
        }

        internal int Count => m_instances.Count;

        internal void Remove(DicomUID instanceUid)
        {
            m_instances.Remove(instanceUid);
        }

        private readonly Dictionary<DicomUID, InstanceAndFileName> m_instances;
    }
}
