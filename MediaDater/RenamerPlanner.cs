namespace MediaDater;

internal class RenamePlanner
{
    private readonly string _baseDirectory;
    private readonly HashSet<string> _allowedExtensions;

    public RenamePlanner(string baseDirectory, IEnumerable<string> allowedExtensions)
    {
        _baseDirectory = baseDirectory;
        _allowedExtensions = [.. allowedExtensions.Select(e => e.ToLowerInvariant())];
    }

    public IEnumerable<RenameBatch> Plan(IEnumerable<FileEntry> entries)
    {
        List<FileEntry> validEntries = GetValidEntries(entries);

        if (validEntries.Count == 0)
        {
            return [];
        }

        IEnumerable<string> directories = GetDirectories(validEntries);
        List<RenameBatch> batches = [];

        foreach (string directory in directories)
        {
            List<FileEntry> directoryFiles = GetDirectoryFiles(validEntries, directory);
            List<IGrouping<string, FileEntry>> groups = GetGroups(directoryFiles);

            foreach (IGrouping<string, FileEntry> group in groups)
            {
                FileNameGenerator generator = new(directory);
                List<FileRenameInfo> renames = [.. generator.Generate(group.Key, group)];

                batches.Add(new(directory, renames));
            }
        }

        return batches;
    }

    private static List<FileEntry> GetDirectoryFiles(List<FileEntry> validEntries, string directory)
    {
        return [.. validEntries.Where(e => Path.GetDirectoryName(e.Path) == directory)];
    }

    private static List<IGrouping<string, FileEntry>> GetGroups(List<FileEntry> directoryFiles)
    {
        return [.. directoryFiles.GroupBy(e => FileNameGenerator.GetDateTimeKey(e.CaptureTime))];
    }

    private static IEnumerable<string> GetDirectories(List<FileEntry> validEntries)
    {
        return validEntries
            .Select(e => Path.GetDirectoryName(e.Path) ?? string.Empty)
            .Distinct();
    }

    private List<FileEntry> GetValidEntries(IEnumerable<FileEntry> entries)
    {
        return [.. entries
            .Where(e => _allowedExtensions.Contains(Path.GetExtension(e.Path).ToLowerInvariant()))
            .Where(e => e.CaptureTime != default)];
    }
}