using System.CommandLine;

namespace LocalSendRenamer;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("LocalSendRenamer – rename media files using Persian calendar capture dates");

        Command renameCommand = new("rename", "Rename files in a directory");

        Argument<DirectoryInfo?> directoryArg = new(
            "directory",
            () => null,
            "Directory to process (defaults to current directory)"
        );

        Option<bool> dryRunOption = new("--dry-run", "Perform a trial run with no changes made");
        renameCommand.AddOption(dryRunOption);

        renameCommand.AddArgument(directoryArg);
        renameCommand.SetHandler(async (context) =>
        {
            DirectoryInfo? dirInfo = context.ParseResult.GetValueForArgument(directoryArg);
            string baseDirectory = dirInfo?.FullName ?? Environment.CurrentDirectory;
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            await RenameOrchestrator.Run(baseDirectory, dryRun);
        });
        rootCommand.AddCommand(renameCommand);

        rootCommand.SetHandler(async () => await RenameOrchestrator.Run(Environment.CurrentDirectory));

        Command installCommand = new("install", "Add the current executable directory to user PATH");
        installCommand.SetHandler(PathInstaller.Install);
        rootCommand.AddCommand(installCommand);

        Command uninstallCommand = new("uninstall", "Remove the current executable directory from user PATH");
        uninstallCommand.SetHandler(PathInstaller.Uninstall);
        rootCommand.AddCommand(uninstallCommand);

        Command undoCommand = new("undo", "Revert the last rename operation in the current directory");
        undoCommand.SetHandler(() => UndoCommand.Run(Environment.CurrentDirectory));
        rootCommand.AddCommand(undoCommand);

        return await rootCommand.InvokeAsync(args);
    }
}