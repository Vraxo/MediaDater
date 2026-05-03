namespace LocalSendRenamer;

public static class PathInstaller
{
    public static void Install()
    {
        string appDir = GetAppDir();
        List<string> paths = GetCurrentPaths();

        if (paths.Contains(appDir, StringComparer.OrdinalIgnoreCase))
        {
            ConsoleLogger.Warning($"Directory already in PATH: {appDir}");
            return;
        }

        paths.Add(appDir);
        SetEnvironmentPaths(paths);

        ConsoleLogger.Ok("Install", "Added to user PATH");
        ConsoleLogger.Info($"Directory: {appDir}");
        ConsoleLogger.Info("Restart your terminal for changes to take effect.");
    }

    public static void Uninstall()
    {
        string appDir = GetAppDir();
        List<string> paths = GetCurrentPaths();

        if (!paths.Contains(appDir, StringComparer.OrdinalIgnoreCase))
        {
            ConsoleLogger.Warning($"Directory not found in PATH: {appDir}");
            return;
        }

        paths.RemoveAll(p => p.Equals(appDir, StringComparison.OrdinalIgnoreCase));
        SetEnvironmentPaths(paths);

        ConsoleLogger.Ok("Uninstall", "Removed from user PATH");
        ConsoleLogger.Info($"Directory: {appDir}");
        ConsoleLogger.Info("Restart your terminal for changes to take effect.");
    }

    private static string GetAppDir()
    {
        return AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    }

    private static List<string> GetCurrentPaths()
    {
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
            ?? string.Empty;

        return currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static void SetEnvironmentPaths(List<string> paths)
    {
        string newPath = string.Join(';', paths);
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
    }
}