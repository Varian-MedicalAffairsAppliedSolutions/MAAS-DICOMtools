using FellowOakDicom;
using Microsoft.Extensions.Logging;

namespace DicomTools.SearchTag
{
    internal static class DicomTagExtensions
    {
        internal static (IReadOnlyList<DicomTag>, string?)? ParseTags(ILogger logger, string tagString)
        {
            var tagKeyValue = tagString.Split('=');
            if (tagKeyValue.Length != 2)
            {
                logger.LogError($"Tag {tagString} is not in expected format.");
                return null;
            }

            var tagPathAsString = tagKeyValue[0];
            var tagValueAsString = tagKeyValue[1];
            if (tagValueAsString == "?")
                tagValueAsString = null;

            var tagPathParts = tagPathAsString.Split('/');
            var dicomTags = new List<DicomTag>();
            foreach (var tagPathPart in tagPathParts)
            {
                var dicomTag = FindTag(tagPathPart);
                if (dicomTag == null)
                {
                    logger.LogError($"Tag {tagPathAsString} not found from dictionary.");
                    return null;
                }
                dicomTags.Add(dicomTag);
            }

            return (dicomTags, tagValueAsString);
        }

        internal static IReadOnlyList<(DicomTag, string?)> FindTags(DicomDataset dataset, IReadOnlyList<DicomTag> tagPath, string? valueToFind)
        {
            var foundList = new List<(DicomTag, string?)>();
            if (tagPath.Count == 1)
            {
                var tag = tagPath[0];
                if (dataset.TryGetString(tag, out var realValue))
                {
                    if (valueToFind == null || realValue == valueToFind)
                        foundList.Add((tag, realValue));
                }

                return foundList;
            }

            var sequenceTag = tagPath[0];
            tagPath = tagPath.Skip(1).ToList();

            if (dataset.Contains(sequenceTag))
            {
                var sequenceDatasets = dataset.GetSequence(sequenceTag).Items;
                foreach (var sequenceDataset in sequenceDatasets)
                    foundList.AddRange(FindTags(sequenceDataset, tagPath, valueToFind));
            }

            return foundList;
        }

        private static DicomTag? FindTag(string tagAsString)
        {
            var dicomDictionary = DicomDictionary.Default;
            var dictionaryEntry = dicomDictionary.SingleOrDefault(e => e.Tag.ToString().Equals(tagAsString, StringComparison.OrdinalIgnoreCase));
            if (dictionaryEntry == null)
                return null;
            return dictionaryEntry.Tag;
        }
    }
}
