using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;

namespace LocalSendRenamer;

internal static class Program
{
    private const int MinPlausibleYear = 2000;
    private const int MaxPlausibleYear = 2030;

    private static readonly string[] AllowedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".mp4", ".jfif"
    ];

    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("LocalSendRenamer – rename media files using Persian calendar capture dates");

        // Rename command (default)
        Command renameCommand = new("rename", "Rename files in a directory");
        Argument<DirectoryInfo?> directoryArg = new(
            "directory",
            () => null,
            "Directory to process (defaults to current directory)"
        );
        renameCommand.AddArgument(directoryArg);
        renameCommand.SetHandler(async (InvocationContext context) =>
        {
            var dirInfo = context.ParseResult.GetValueForArgument(directoryArg);
            string baseDirectory = dirInfo?.FullName ?? Environment.CurrentDirectory;
            await RunRename(baseDirectory);
        });
        rootCommand.AddCommand(renameCommand);

        // Default command (no subcommand): run rename on current directory
        rootCommand.SetHandler(async () => await RunRename(Environment.CurrentDirectory));

        // Install command
        Command installCommand = new("install", "Add the current executable directory to user PATH");
        installCommand.SetHandler(InstallToPath);
        rootCommand.AddCommand(installCommand);

        // Uninstall command
        Command uninstallCommand = new("uninstall", "Remove the current executable directory from user PATH");
        uninstallCommand.SetHandler(UninstallFromPath);
        rootCommand.AddCommand(uninstallCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task RunRename(string baseDirectory)
    {
        Console.WriteLine($"Processing directory: {baseDirectory}\n");
        Stopwatch stopwatch = Stopwatch.StartNew();

        var (entries, skippedCount) = FileScanner.Scan(baseDirectory, AllowedExtensions);
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

    private static void InstallToPath()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        var paths = currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (paths.Contains(appDir, StringComparer.OrdinalIgnoreCase))
        {
            ConsoleLogger.Warning($"Directory already in PATH: {appDir}");
            return;
        }

        paths.Add(appDir);
        string newPath = string.Join(';', paths);
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);

        ConsoleLogger.Ok("Install", "Added to user PATH");
        ConsoleLogger.Info($"Directory: {appDir}");
        ConsoleLogger.Info("Restart your terminal for changes to take effect.");
    }

    private static void UninstallFromPath()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        var paths = currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (!paths.Contains(appDir, StringComparer.OrdinalIgnoreCase))
        {
            ConsoleLogger.Warning($"Directory not found in PATH: {appDir}");
            return;
        }

        paths.RemoveAll(p => p.Equals(appDir, StringComparison.OrdinalIgnoreCase));
        string newPath = string.Join(';', paths);
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);

        ConsoleLogger.Ok("Uninstall", "Removed from user PATH");
        ConsoleLogger.Info($"Directory: {appDir}");
        ConsoleLogger.Info("Restart your terminal for changes to take effect.");
    }
}