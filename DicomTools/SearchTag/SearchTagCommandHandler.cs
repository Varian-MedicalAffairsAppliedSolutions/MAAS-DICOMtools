using DicomTools.CommandLineExtensions;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.IO;

namespace DicomTools.SearchTag
{
    public class SearchTagCommandHandler(ILogger<SearchTagCommandHandler> m_logger, IConsole m_console) : ICommandOptionsHandler<SearchTagOptions>
    {
        public async Task<int> HandleAsync(SearchTagOptions options, CancellationToken cancellationToken)
        {
            var tagValues = options.Tag;
            var path = options.Path;
            var searchPattern = options.SearchPattern;
            var showOnlyDirectories = options.ShowOnlyDirectories;
            var showStatistics = options.ShowStatistics;

            try
            {
                var dicomTags = new List<(IReadOnlyList<DicomTag>, string?)>();
                foreach (var tagValue in tagValues)
                {
                    var dicomTag = DicomTagExtensions.ParseTags(m_logger, tagValue);
                    if (!dicomTag.HasValue)
                        return 2;
                    dicomTags.Add(dicomTag.Value);
                }

                var fileFoundCount = 0;
                var fileTotalCount = 0;
                var (_, filesFound) = FindFromFiles(m_logger, m_console, path, searchPattern,
                    dicomTags, showOnlyDirectories, ref fileFoundCount, ref fileTotalCount);
                if (filesFound.Count == 0)
                {
                    m_console.Out.WriteLine("No files found.");
                    return 2;
                }

                if (showStatistics)
                    m_console.Out.WriteLine($"{fileFoundCount}/{fileTotalCount}");
                return await Task.FromResult(0);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex.Message);
                return await Task.FromResult(1);
            }
        }

        internal static (List<string> directories, List<(string, IReadOnlyList<(DicomTag, string?)>)> files) FindFromFiles(
            ILogger logger, IConsole console, string path, string searchPattern,
            IReadOnlyList<(IReadOnlyList<DicomTag>, string?)> tagValues, bool showOnlyDirectories, ref int fileFoundCount, ref int fileTotalCount)
        {
            var directoryList = new List<string>();
            var fileList = new List<(string, IReadOnlyList<(DicomTag, string?)>)>();

            var files = Directory.EnumerateFiles(path, searchPattern);
            foreach (var file in files)
            {
                fileTotalCount++;

                try
                {
                    var dicomFile = DicomFile.Open(file, FileReadOption.SkipLargeTags);
                    var tagCountToFind = tagValues.Count;
                    var foundTagValues = new List<(DicomTag, string?)>();
                    foreach (var tagValue in tagValues)
                    {
                        var tags = DicomTagExtensions.FindTags(dicomFile.Dataset, tagValue.Item1, tagValue.Item2);
                        foundTagValues.AddRange(tags);
                    }
                    if (foundTagValues.Count >= tagCountToFind) // May find more than one under sequences.
                    {
                        fileFoundCount++;
                        fileList.Add((file, foundTagValues));
                        if (!showOnlyDirectories)
                        {
                            foreach (var foundTagValue in foundTagValues)
                                console.Out.WriteLine(file + ": " + foundTagValue.Item1 + "=" + foundTagValue.Item2);
                        }
                    }
                }
                catch (DicomFileException ex)
                {
                    logger.LogError($"File {file} is not a dicom file. ({ex.Message})");
                }
            }

            if (fileList.Count > 0)
            {
                directoryList.Add(path);
                if (showOnlyDirectories)
                    console.Out.WriteLine(path);
            }

            var directories = Directory.EnumerateDirectories(path);
            foreach (var directory in directories)
            {
                var (subDirectories, subFiles) = FindFromFiles(logger, console, directory, searchPattern,
                    tagValues, showOnlyDirectories, ref fileFoundCount, ref fileTotalCount);
                directoryList.AddRange(subDirectories);
                fileList.AddRange(subFiles);
            }

            return (directoryList, fileList);
        }
    }
}