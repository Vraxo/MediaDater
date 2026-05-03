using System.Text.RegularExpressions;

namespace LocalSendRenamer;

public static class FileScanner
{
    private static readonly Regex AlreadyRenamedRegex = new(@"^\d{4}-\d{2}-\d{2}(?:-\d+)?$", RegexOptions.Compiled);

    private static IEnumerable<string> GetDirectories(string baseDirectory)
    {
        return Directory.EnumerateDirectories(baseDirectory, "*", SearchOption.AllDirectories)
            .Prepend(baseDirectory);
    }

    private static bool IsAlreadyRenamed(string filePath)
    {
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        return AlreadyRenamedRegex.IsMatch(baseName);
    }

    public static (List<FileEntry> Entries, int SkippedCount) Scan(string baseDirectory, string[] allowedExtensions)
    {
        HashSet<string> allowedSet = [.. allowedExtensions.Select(e => e.ToLowerInvariant())];
        List<FileEntry> entries = [];
        int skippedCount = 0;

        foreach (string dir in GetDirectories(baseDirectory))
        {
            foreach (string file in Directory.EnumerateFiles(dir))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();

                if (!allowedSet.Contains(ext))
                {
                    continue;
                }

                if (IsAlreadyRenamed(file))
                {
                    ConsoleLogger.Skip(Path.GetFileName(file));
                    skippedCount++;
                    continue;
                }

                DateTime? date = CaptureDateExtractor.Extract(file);

                if (!date.HasValue)
                {
                    DateTime creation = File.GetCreationTime(file);
                    DateTime modified = File.GetLastWriteTime(file);
                    DateTime fallbackDate = creation < modified ? creation : modified;

                    ConsoleLogger.Warning($"Using file system date ({fallbackDate:yyyy-MM-dd}) as capture date for: {Path.GetFileName(file)}");
                    entries.Add(new FileEntry(file, fallbackDate));
                    continue;
                }

                entries.Add(new FileEntry(file, date.Value));
            }
        }

        return (entries, skippedCount);
    }
}