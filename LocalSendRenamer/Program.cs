using System.Diagnostics;

namespace LocalSendRenamer;

internal class Program
{
    private const string BaseDirectory = @"C:\Users\Parsa\Downloads\LocalSend";
    private const int MinPlausibleYear = 2000;
    private const int MaxPlausibleYear = 2030;

    private static readonly string[] AllowedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".mp4", ".jfif"
    ];

    private static void Main()
    {
        Console.WriteLine("Starting rename process...\n");
        Stopwatch stopwatch = Stopwatch.StartNew();

        (List<FileEntry>? entries, int skippedCount) = FileScanner.Scan(BaseDirectory, AllowedExtensions);
        int totalScanned = skippedCount + entries.Count;

        if (!DateValidator.AreAllPlausible(entries, MinPlausibleYear, MaxPlausibleYear))
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
                if (RenameExecutor.Execute(rename))
                {
                    totalRenamed++;
                }
            }
        }

        stopwatch.Stop();

        // All summary lines together, aligned
        Console.WriteLine();
        ConsoleLogger.AlignedInfo("Total", totalScanned.ToString());
        ConsoleLogger.AlignedInfo("Skipped", skippedCount.ToString());
        ConsoleLogger.Success("Renamed", totalRenamed.ToString());
        ConsoleLogger.Success("Time", stopwatch.Elapsed.ToString());
        Console.ReadLine();
    }
}