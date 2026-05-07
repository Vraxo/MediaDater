
namespace LocalSendRenamer;

public static class UndoCommand
{
    public static void Run(string directory)
    {
        List<RenameManifestEntry> entries = ManifestManager.Load(directory);
        if (entries.Count == 0)
        {
            ConsoleLogger.Warning("No manifest found. Nothing to undo.");
            return;
        }

        string fileWord = entries.Count == 1 ? "file" : "files";
        ConsoleLogger.Info($"Found {entries.Count} {fileWord} to revert.\n");
        int undone = 0;

        foreach (RenameManifestEntry entry in entries)
        {
            string originalName = Path.GetFileName(entry.OriginalPath);
            string newName = Path.GetFileName(entry.NewPath);

            if (!File.Exists(entry.NewPath))
            {
                ConsoleLogger.Warning($"New file missing, cannot revert: {newName}");
                continue;
            }

            if (File.Exists(entry.OriginalPath))
            {
                ConsoleLogger.Warning($"Original already exists, skipping revert: {originalName}");
                continue;
            }

            try
            {
                File.Move(entry.NewPath, entry.OriginalPath);
                ConsoleLogger.Ok(originalName, newName); // reversed: original -> new
                undone++;
            }
            catch (Exception ex)
            {
                ConsoleLogger.Error($"Failed to revert {newName}: {ex.Message}");
            }
        }

        if (undone == entries.Count)
        {
            ManifestManager.Delete(directory);
            Console.WriteLine();
            ConsoleLogger.Success("Undone", undone.ToString());
        }
        else if (undone > 0)
        {
            Console.WriteLine();
            ConsoleLogger.Warning(
                $"Partially undone: {undone} of {entries.Count} files." +
                $"Manifest preserved for retry.");
        }
        else
        {
            ConsoleLogger.Warning("No files were reverted.");
        }
    }
}