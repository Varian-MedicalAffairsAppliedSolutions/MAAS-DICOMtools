using System.Collections;
using FellowOakDicom;

namespace DicomTools.DataModel.CollectFiles
{
    internal class ReadOnlyCollectedSeries : IEnumerable<(DicomUID SeriesUid, Modality Modality, CollectedInstances Instances)>
    {
        internal ReadOnlyCollectedSeries(Instance instance, string fileName)
        {
            m_collectedSeries = new Dictionary<DicomUID, (Modality, CollectedInstances)>
            {
                { instance.SeriesUid, (instance.Modality, new CollectedInstances(instance, fileName)) }
            };
        }

        internal bool TryGetValue(DicomUID seriesUid, out CollectedInstances? instances)
        {
            var found = m_collectedSeries.TryGetValue(seriesUid, out (Modality Modality, CollectedInstances Instances) modalityInstances);
            instances = found ? modalityInstances.Instances : null;
            return found;
        }

        public IEnumerator<(DicomUID SeriesUid, Modality Modality, CollectedInstances Instances)> GetEnumerator()
        {
            var collectedSeriesList = new List<(DicomUID, Modality, CollectedInstances)>();
            foreach (var collectedSeries in m_collectedSeries)
            {
                collectedSeriesList.Add((collectedSeries.Key, collectedSeries.Value.Modality, collectedSeries.Value.Instances));
            }
            return collectedSeriesList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_collectedSeries.GetEnumerator();
        }

        internal int Count => m_collectedSeries.Count;

        internal IReadOnlyList<(DicomUID SeriesUid, Modality Modality, CollectedInstances Instances)> Find(ModalityType modalityType)
        {
            return m_collectedSeries.Where(s => s.Value.Modality.Type == modalityType)
                .Select(s => (s.Key, s.Value.Modality, s.Value.Instances)).ToList();
        }

        internal IReadOnlyList<InstanceAndFileName> FindInstances(ModalityType modalityType, Func<InstanceAndFileName, bool> compareFunc)
        {
            var series = this.Where(s => s.Modality.Type == modalityType).ToList();
            var instances = series.SelectMany(s => s.Instances)
                .Where(i => compareFunc(i.InstanceAndFileName))
                .Select(i => i.InstanceAndFileName).ToList();
            return instances;
        }

        protected readonly Dictionary<DicomUID, (Modality Modality, CollectedInstances Instances)> m_collectedSeries;
    }
}
