
namespace LocalSendRenamer;

public static class PathInstaller
{
    public static void Install()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        List<string> paths = currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

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

    public static void Uninstall()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        List<string> paths = currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

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