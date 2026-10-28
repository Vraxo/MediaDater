using System.CommandLine;

namespace MediaDater;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("MediaDater – rename media files using Persian calendar capture dates");

        Command renameCommand = new("rename", "Rename files in a directory");

        Argument<DirectoryInfo?> directoryArg = new("directory")
        {
            Description = "Directory to process (defaults to current directory)",
            Arity = ArgumentArity.ZeroOrOne
        };

        Option<bool> dryRunOption = new("--dry-run")
        {
            Description = "Perform a trial run with no changes made"
        };

        renameCommand.Options.Add(dryRunOption);
        renameCommand.Arguments.Add(directoryArg);

        renameCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            DirectoryInfo? dirInfo = parseResult.GetValue(directoryArg);
            string baseDirectory = dirInfo?.FullName ?? Environment.CurrentDirectory;
            bool dryRun = parseResult.GetValue(dryRunOption);
            await RenameOrchestrator.Run(baseDirectory, dryRun);
        });

        rootCommand.Subcommands.Add(renameCommand);

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
            await RenameOrchestrator.Run(Environment.CurrentDirectory));

        Command installCommand = new("install", "Add the current executable directory to user PATH");
        installCommand.SetAction(parseResult => PathInstaller.Install());
        rootCommand.Subcommands.Add(installCommand);

        Command uninstallCommand = new("uninstall", "Remove the current executable directory from user PATH");
        uninstallCommand.SetAction(parseResult => PathInstaller.Uninstall());
        rootCommand.Subcommands.Add(uninstallCommand);

        Command undoCommand = new("undo", "Revert the last rename operation in the current directory");
        undoCommand.SetAction(parseResult => UndoCommand.Run(Environment.CurrentDirectory));
        rootCommand.Subcommands.Add(undoCommand);

        ParseResult parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }
}