using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LocalSendRenamer;

internal partial class Program
{
    private const string BaseDirectory = @"C:\Users\Parsa\Downloads\LocalSend";

    private static readonly string[] AllowedExtensions =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
        ".mp4",
        ".jfif"
    ];

    private const int MinPlausibleYear = 2000;
    private const int MaxPlausibleYear = 2030;

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}(?:-\d+)?$")]
    private static partial Regex AlreadyRenamedRegex();

    private static void Main()
    {
        Console.WriteLine("Starting rename process...\n");

        Stopwatch stopwatch = Stopwatch.StartNew();
        IEnumerable<FileEntry> entries = CollectFileEntries(BaseDirectory, AllowedExtensions);

        if (!AreAllDatesPlausible(entries))
        {
            ConsoleLogger.Error("\nAborting: no files were renamed.");
            Console.ReadLine();
            return;
        }

        RenamePlanner planner = new(BaseDirectory, AllowedExtensions);
        IEnumerable<RenameBatch> batches = planner.Plan(entries);

        int totalRenamed = 0;

        foreach (RenameBatch batch in batches)
        {
            foreach (FileRenameInfo rename in batch.Renames)
            {
                ExecuteRename(rename);
                totalRenamed++;
            }
        }

        stopwatch.Stop();

        Console.WriteLine();
        ConsoleLogger.Success("Total Renamed", totalRenamed.ToString());
        ConsoleLogger.Success("Execution Time", stopwatch.Elapsed.ToString());
        Console.ReadLine();
    }

    private static IEnumerable<FileEntry> CollectFileEntries(string baseDirectory, string[] allowedExtensions)
    {
        HashSet<string> allowedSet = [.. allowedExtensions.Select(e => e.ToLowerInvariant())];

        foreach (string? dir in GetDirectories(baseDirectory))
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
                    continue;
                }

                DateTime? date = CaptureDateExtractor.Extract(file);

                if (!date.HasValue)
                {
                    ConsoleLogger.Warning($"Could not extract capture date from: {Path.GetFileName(file)}");
                    continue;
                }

                yield return new(file, date.Value);
            }
        }
    }

    private static IEnumerable<string> GetDirectories(string baseDirectory)
    {
        return Directory.EnumerateDirectories(baseDirectory, "*", SearchOption.AllDirectories)
            .Prepend(baseDirectory);
    }

    private static void ExecuteRename(FileRenameInfo rename)
    {
        if (PathsAreSame(rename))
        {
            return;
        }

        string originalName = Path.GetFileName(rename.OriginalPath);

        try
        {
            File.Move(rename.OriginalPath, rename.NewPath);
            string newName = Path.GetFileName(rename.NewPath);
            ConsoleLogger.Ok(originalName, newName);
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"{originalName}: {ex.Message}");
        }
    }

    private static bool PathsAreSame(FileRenameInfo rename)
    {
        string originalPath = Path.GetFullPath(rename.OriginalPath);
        string newPath = Path.GetFullPath(rename.NewPath);
        return originalPath.Equals(newPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool AreAllDatesPlausible(IEnumerable<FileEntry> entries)
    {
        bool allOk = true;
        foreach (FileEntry entry in entries)
        {
            if (entry.CaptureTime.Year is < MinPlausibleYear or > MaxPlausibleYear)
            {
                ConsoleLogger.Error($"Implausible date {entry.CaptureTime:yyyy-MM-dd} for file: {entry.Path}");
                allOk = false;
            }
        }
        return allOk;
    }

    private static bool IsAlreadyRenamed(string filePath)
    {
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        return AlreadyRenamedRegex().IsMatch(baseName);
    }
}