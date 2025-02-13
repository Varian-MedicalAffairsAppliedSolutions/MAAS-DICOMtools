using System.Collections;

namespace DicomTools.DataModel.CollectFiles
{
    internal class CollectedPatientSeries : IEnumerable<(string PatientId, CollectedSeries CollectedSeries)>
    {
        internal CollectedPatientSeries()
        {
            m_collectedSeries = new Dictionary<string, CollectedSeries>();
        }

        internal CollectedPatientSeries(string patientId, Instance instance, string fileName)
        {
            m_collectedSeries = new Dictionary<string, CollectedSeries>
            {
                { patientId, new CollectedSeries(instance, fileName) }
            };
        }

        internal bool TryGetValue(string patientId, out CollectedSeries? series)
        {
            return m_collectedSeries.TryGetValue(patientId, out series);
        }

        internal void Add(string patientId, Instance instance, string fileName)
        {
            if (TryGetValue(patientId, out var collectedSeries))
                collectedSeries!.Add(instance, fileName);
            else
                m_collectedSeries.Add(patientId, new CollectedSeries(instance, fileName));
        }

        public IEnumerator<(string PatientId, CollectedSeries CollectedSeries)> GetEnumerator()
        {
            var collectedSeries = new List<(string, CollectedSeries)>();
            foreach (var collectedInstance in m_collectedSeries)
            {
                collectedSeries.Add((collectedInstance.Key, collectedInstance.Value));
            }
            return collectedSeries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_collectedSeries.GetEnumerator();
        }

        internal int Count => m_collectedSeries.Count;

        private readonly Dictionary<string, CollectedSeries> m_collectedSeries;
    }
}
