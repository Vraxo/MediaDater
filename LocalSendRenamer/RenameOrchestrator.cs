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

        IEnumerable<RenameBatch> batches = new RenamePlanner(baseDirectory, AllowedExtensions).Plan(entries);
        (int totalRenamed, List<FileRenameInfo>? allRenames) = ExecuteRenames(batches);

        if (totalRenamed > 0)
        {
            ManifestManager.Save(baseDirectory, allRenames);
        }

        stopwatch.Stop();
        PrintSummary(totalScanned, skippedCount, totalRenamed, stopwatch.Elapsed);
        Console.ReadLine();
    }

    private static (int totalRenamed, List<FileRenameInfo> allRenames) ExecuteRenames(IEnumerable<RenameBatch> batches)
    {
        List<FileRenameInfo> allRenames = [];
        int totalRenamed = 0;

        foreach (RenameBatch batch in batches)
        {
            foreach (FileRenameInfo rename in batch.Renames)
            {
                allRenames.Add(rename);

                if (!RenameExecutor.Execute(rename))
                {
                    continue;
                }

                totalRenamed++;
            }
        }

        return (totalRenamed, allRenames);
    }

    private static void PrintSummary(int totalScanned, int skippedCount, int totalRenamed, TimeSpan elapsed)
    {
        if (totalScanned > 0)
        {
            Console.WriteLine();
        }

        ConsoleLogger.AlignedInfo("Total", totalScanned.ToString());
        ConsoleLogger.AlignedInfo("Skipped", skippedCount.ToString());
        ConsoleLogger.Success("Renamed", totalRenamed.ToString());
        ConsoleLogger.Success("Time", elapsed.ToString());
    }
}