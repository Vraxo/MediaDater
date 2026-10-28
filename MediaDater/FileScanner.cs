using System.Text.RegularExpressions;

namespace MediaDater;

internal static partial class FileScanner
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}(?:_\d{2}-\d{2}-\d{2})?(?:-\d+)?$", RegexOptions.Compiled)]
    private static partial Regex AlreadyRenamedRegex();

    private static IEnumerable<string> GetDirectories(string baseDirectory)
    {
        return Directory.EnumerateDirectories(baseDirectory, "*", SearchOption.AllDirectories)
            .Prepend(baseDirectory);
    }

    private static bool IsAlreadyRenamed(string filePath)
    {
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        return AlreadyRenamedRegex().IsMatch(baseName);
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
                (FileEntry? entry, bool skipped) = ProcessFile(file, allowedSet);

                if (skipped)
                {
                    skippedCount++;
                }
                else if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
        }

        return (entries, skippedCount);
    }

    private static (FileEntry? Entry, bool Skipped) ProcessFile(string filePath, HashSet<string> allowedExtensions)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
        {
            return (null, false);
        }

        if (IsAlreadyRenamed(filePath))
        {
            ConsoleLogger.Skip(Path.GetFileName(filePath));
            return (null, true);
        }

        DateTime? date = CaptureDateExtractor.Extract(filePath);

        if (date.HasValue)
        {
            return (new(filePath, date.Value), false);
        }

        DateTime creation = File.GetCreationTime(filePath);
        DateTime modified = File.GetLastWriteTime(filePath);

        DateTime fallbackDate = creation < modified
            ? creation
            : modified;

        ConsoleLogger.Warning($"Using file system date ({fallbackDate:yyyy-MM-dd}) as capture date for: {Path.GetFileName(filePath)}");

        return (new(filePath, fallbackDate), false);
    }
}