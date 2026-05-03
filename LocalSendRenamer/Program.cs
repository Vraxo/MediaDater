using System.CommandLine;
using System.CommandLine.Invocation;

namespace LocalSendRenamer;

internal static class Program
{
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
            DirectoryInfo? dirInfo = context.ParseResult.GetValueForArgument(directoryArg);
            string baseDirectory = dirInfo?.FullName ?? Environment.CurrentDirectory;
            await RenameOrchestrator.Run(baseDirectory);
        });
        rootCommand.AddCommand(renameCommand);

        // Default command (no subcommand): run rename on current directory
        rootCommand.SetHandler(async () => await RenameOrchestrator.Run(Environment.CurrentDirectory));

        // Install command
        Command installCommand = new("install", "Add the current executable directory to user PATH");
        installCommand.SetHandler(PathInstaller.Install);
        rootCommand.AddCommand(installCommand);

        // Uninstall command
        Command uninstallCommand = new("uninstall", "Remove the current executable directory from user PATH");
        uninstallCommand.SetHandler(PathInstaller.Uninstall);
        rootCommand.AddCommand(uninstallCommand);

        return await rootCommand.InvokeAsync(args);
    }
}