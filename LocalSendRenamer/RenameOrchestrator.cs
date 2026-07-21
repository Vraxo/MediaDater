using System.Diagnostics;

namespace LocalSendRenamer;

public static class RenameOrchestrator
{
    private static readonly string[] AllowedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".mp4", ".jfif"
    ];

    public static async Task Run(string baseDirectory, bool dryRun = false)
    {
        if (dryRun)
        {
            ConsoleLogger.Info("DRY RUN MODE – no files will be changed.\n");
        }

        Console.WriteLine($"Processing directory: {baseDirectory}\n");
        Stopwatch stopwatch = Stopwatch.StartNew();

        (List<FileEntry> entries, int skippedCount) = FileScanner.Scan(baseDirectory, AllowedExtensions);
        int totalScanned = skippedCount + entries.Count;

        if (!DateValidator.AreAllPlausible(entries))
        {
            ConsoleLogger.Error("\nAborting: no files were renamed.");
            Console.ReadLine();
            return;
        }

        IEnumerable<RenameBatch> batches = new RenamePlanner(baseDirectory, AllowedExtensions).Plan(entries);
        (int totalRenamed, List<FileRenameInfo> allRenames) = ExecuteRenames(batches, dryRun);

        if (!dryRun && totalRenamed > 0)
        {
            ManifestManager.Save(baseDirectory, allRenames);
        }

        stopwatch.Stop();
        PrintSummary(totalScanned, skippedCount, totalRenamed, stopwatch.Elapsed, dryRun);
        Console.ReadLine();
    }

    private static (int totalRenamed, List<FileRenameInfo> allRenames) ExecuteRenames(IEnumerable<RenameBatch> batches, bool dryRun)
    {
        List<FileRenameInfo> allRenames = [];
        int totalRenamed = 0;

        foreach (RenameBatch batch in batches)
        {
            foreach (FileRenameInfo rename in batch.Renames)
            {
                allRenames.Add(rename);

                if (dryRun)
                {
                    ConsoleLogger.Info($"[DRY RUN] Would rename: {Path.GetFileName(rename.OriginalPath)} -> {Path.GetFileName(rename.NewPath)}");
                    totalRenamed++;
                    continue;
                }

                if (!RenameExecutor.Execute(rename))
                {
                    continue;
                }

                totalRenamed++;
            }
        }

        return (totalRenamed, allRenames);
    }

    private static void PrintSummary(int totalScanned, int skippedCount, int totalRenamed, TimeSpan elapsed, bool dryRun)
    {
        if (totalScanned > 0)
        {
            Console.WriteLine();
        }

        ConsoleLogger.LogAligned("Total", totalScanned.ToString());
        ConsoleLogger.LogAligned("Skipped", skippedCount.ToString());
        ConsoleLogger.LogAligned("Renamed", totalRenamed.ToString());
        ConsoleLogger.LogAligned("Time", elapsed.ToString());

        if (!dryRun)
        {
            return;
        }

        ConsoleLogger.Info("\nDry run complete – no files were actually changed.");
    }
}