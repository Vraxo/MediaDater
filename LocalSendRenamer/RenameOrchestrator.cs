using System.Diagnostics;

namespace LocalSendRenamer;

public static class RenameOrchestrator
{
    private const int MinPlausibleYear = 2000;
    private const int MaxPlausibleYear = 2030;

    private static readonly string[] AllowedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".mp4", ".jfif"
    ];

    public static async Task Run(string baseDirectory)
    {
        Console.WriteLine($"Processing directory: {baseDirectory}\n");
        Stopwatch stopwatch = Stopwatch.StartNew();

        (List<FileEntry>? entries, int skippedCount) = FileScanner.Scan(baseDirectory, AllowedExtensions);
        int totalScanned = skippedCount + entries.Count;

        if (!DateValidator.AreAllPlausible(entries, MinPlausibleYear, MaxPlausibleYear))
        {
            ConsoleLogger.Error("\nAborting: no files were renamed.");
            Console.ReadLine();
            return;
        }

        RenamePlanner planner = new(baseDirectory, AllowedExtensions);
        IEnumerable<RenameBatch> batches = planner.Plan(entries);
        int totalRenamed = 0;

        foreach (RenameBatch batch in batches)
        {
            foreach (FileRenameInfo rename in batch.Renames)
            {
                if (RenameExecutor.Execute(rename))
                {
                    totalRenamed++;
                }
            }
        }

        stopwatch.Stop();

        Console.WriteLine();
        ConsoleLogger.AlignedInfo("Total", totalScanned.ToString());
        ConsoleLogger.AlignedInfo("Skipped", skippedCount.ToString());
        ConsoleLogger.Success("Renamed", totalRenamed.ToString());
        ConsoleLogger.Success("Time", stopwatch.Elapsed.ToString());
        Console.ReadLine();
    }
}