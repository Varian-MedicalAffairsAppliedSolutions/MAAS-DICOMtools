using FellowOakDicom;

namespace DicomTools.DataModel.CollectFiles
{
    internal class CollectedSeries : ReadOnlyCollectedSeries
    {
        internal CollectedSeries(Instance instance, string fileName) : base(instance, fileName)
        {
        }

        internal void Add(Instance instance, string fileName)
        {
            if (m_collectedSeries.TryGetValue(instance.SeriesUid, out (Modality Modality, CollectedInstances Instances) modalityInstances))
            {
                if (!instance.Modality.Equals(modalityInstances.Modality))
                    throw new ArgumentException($"Instance modality {instance.Modality} does not match with series modality {modalityInstances.Modality}.", nameof(instance));
                modalityInstances.Instances.Add(instance, fileName);
                return;
            }

            m_collectedSeries.Add(instance.SeriesUid, (instance.Modality, new CollectedInstances(instance, fileName)));
        }

        internal IReadOnlyList<(DicomUID SeriesUid, Modality Modality, CollectedInstances Instances)> FindAndRemove(ModalityType modalityType)
        {
            var series = Find(modalityType);
            foreach (var seriesTuple in series)
                m_collectedSeries.Remove(seriesTuple.SeriesUid);
            return series;
        }

        internal void RemoveInstances(IReadOnlyList<Instance> instances)
        {
            foreach (var instance in instances)
            {
                var seriesUid = instance.SeriesUid;
                if (m_collectedSeries.TryGetValue(seriesUid, out var value))
                {
                    value.Instances.Remove(instance.InstanceUid);
                    if (value.Instances.Count == 0)
                        m_collectedSeries.Remove(seriesUid);
                }
            }
        }

        internal IReadOnlyList<InstanceAndFileName> FindAndRemoveInstances(ModalityType modalityType, Func<InstanceAndFileName, bool> compareFunc)
        {
            var instances = FindInstances(modalityType, compareFunc);
            RemoveInstances(instances.Select(i => i.Instance).ToList());
            return instances;
        }

        internal void Remove(DicomUID seriesUid)
        {
            m_collectedSeries.Remove(seriesUid);
        }
    }
}
