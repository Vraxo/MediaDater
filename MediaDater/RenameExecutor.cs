namespace MediaDater;

public static class RenameExecutor
{
    private static bool PathsAreSame(FileRenameInfo rename)
    {
        string originalPath = Path.GetFullPath(rename.OriginalPath);
        string newPath = Path.GetFullPath(rename.NewPath);
        return originalPath.Equals(newPath, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Execute(FileRenameInfo rename)
    {
        if (PathsAreSame(rename))
        {
            return false;
        }

        string originalName = Path.GetFileName(rename.OriginalPath);

        try
        {
            File.Move(rename.OriginalPath, rename.NewPath);
            string newName = Path.GetFileName(rename.NewPath);
            ConsoleLogger.Ok(originalName, newName);
            return true;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"{originalName}: {ex.Message}");
            return false;
        }
    }
}