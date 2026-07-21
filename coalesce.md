### `LocalSendRenamer\CaptureDateExtractor.cs`

```csharp
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalSendRenamer;

public partial class CaptureDateExtractor
{
    [GeneratedRegex(@"^(IMG|VID)_(\d{8})_(\d{6})", RegexOptions.IgnoreCase)]
    private static partial Regex TimestampRegex();

    [GeneratedRegex(@"^Screen(?:shot|recorder)[-_](\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{3})", RegexOptions.IgnoreCase)]
    private static partial Regex ScreenshotRegex();

    public static DateTime? Extract(string filePath)
    {
        DateTime? date = GetMetadataDate(filePath);
        if (date.HasValue && IsPlausible(date.Value))
        {
            return date;
        }

        date = GetFilenameDate(filePath);
        if (date.HasValue && IsPlausible(date.Value))
        {
            return date;
        }

        return null;
    }

    private static bool IsPlausible(DateTime date)
    {
        return date.Year is >= DateValidator.MinPlausibleYear and <= DateValidator.MaxPlausibleYear;
    }

    private static DateTime? GetFilenameDate(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        return ParseStandardTimestamp(fileName)
            ?? ParseScreenshotTimestamp(fileName);
    }

    private static DateTime? ParseStandardTimestamp(string fileName)
    {
        Match match = TimestampRegex().Match(fileName);

        if (!match.Success)
        {
            return null;
        }

        string timestamp = match.Groups[2].Value + match.Groups[3].Value;
        return TryParseDate(timestamp, "yyyyMMddHHmmss");
    }

    private static DateTime? ParseScreenshotTimestamp(string fileName)
    {
        Match match = ScreenshotRegex().Match(fileName);

        if (!match.Success)
        {
            return null;
        }

        return TryParseDate(match.Groups[1].Value, "yyyy-MM-dd-HH-mm-ss-fff");
    }

    private static DateTime? TryParseDate(string input, string format)
    {
        bool success = DateTime.TryParseExact(
            input,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime result);

        return success ? result : null;
    }

    private static DateTime? GetMetadataDate(string filePath)
    {
        try
        {
            IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(filePath);

            return GetExifDate(directories)
                ?? GetQuickTimeDate(directories);
        }
        catch (Exception ex) when (ex is ImageProcessingException or FileNotFoundException)
        {
            return null;
        }
    }

    private static DateTime? GetExifDate(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        ExifSubIfdDirectory? subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        if (subIfd?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime date) == true)
        {
            return date;
        }

        ExifIfd0Directory? ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        if (ifd0?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out date) == true)
        {
            return date;
        }

        return null;
    }

    private static DateTime? GetQuickTimeDate(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        QuickTimeMovieHeaderDirectory? movieHeader = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
        if (movieHeader?.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out DateTime date) ?? false)
        {
            return DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime();
        }

        return null;
    }
}
```

---

### `LocalSendRenamer\ConsoleLogger.cs`

```csharp
namespace LocalSendRenamer;

public static class ConsoleLogger
{
    // Catppuccin Mocha ANSI colors
    private const string Reset = "\x1b[0m";
    private const string White = "\x1b[38;2;255;255;255m";
    private const string Teal = "\x1b[38;2;148;226;213m";
    private const string Blue = "\x1b[38;2;137;180;250m";
    private const string Green = "\x1b[38;2;166;218;149m";
    private const string Yellow = "\x1b[38;2;249;226;175m";
    private const string Red = "\x1b[38;2;243;139;168m";
    private const string Mauve = "\x1b[38;2;203;166;247m";   // #cba6f7

    // Semantic mappings
    private const string ColorSuccess = Green;
    private const string ColorError = Red;
    private const string ColorWarning = Yellow;
    private const string ColorInfo = Blue;
    private const string ColorFilename = Teal;
    private const string ColorSummary = Mauve;   // used for summary labels

    // Fixed width for the entire tag including brackets (e.g., "[ERROR]" = 7)
    private const int TagWidth = 7;
    private const int AlignWidth = 7; // for summary lines

    // Helper: write left-aligned tag, then pad spaces after the bracket to reach TagWidth
    private static void WriteTag(string color, string text)
    {
        Console.Write("[");
        Console.Write(color);
        Console.Write(text);
        Console.Write(Reset);
        Console.Write("]");
        int visibleLength = text.Length + 2; // +2 for brackets
        int padding = TagWidth - visibleLength;
        if (padding > 0)
        {
            Console.Write(new string(' ', padding));
        }
        // No extra space – message starts immediately after padding
    }

    public static void Ok(string originalName, string newName)
    {
        WriteTag(ColorSuccess, "OK");
        Console.Write(ColorFilename);
        Console.Write(originalName);
        Console.Write(Reset);
        Console.Write(" -> ");
        Console.Write(ColorFilename);
        Console.Write(newName);
        Console.Write(Reset);
        Console.WriteLine();
    }

    public static void Error(string message)
    {
        WriteTag(ColorError, "ERROR");
        Console.Write(ColorError);
        Console.WriteLine(message);
        Console.Write(Reset);
    }

    public static void Warning(string message)
    {
        WriteTag(ColorWarning, "WARN");
        Console.Write(ColorWarning);
        Console.WriteLine(message);
        Console.Write(Reset);
    }

    public static void Info(string message)
    {
        Console.Write(ColorInfo);
        Console.WriteLine(message);
        Console.Write(Reset);
    }

    public static void AlignedInfo(string label, string value)
    {
        string paddedLabel = label.PadRight(AlignWidth);
        Console.Write(ColorSummary);
        Console.Write(paddedLabel);
        Console.Write(": ");
        Console.Write(White);
        Console.Write(value);
        Console.Write(Reset);
        Console.WriteLine();
    }

    public static void Success(string label, string value)
    {
        string paddedLabel = label.PadRight(AlignWidth);
        Console.Write(ColorSummary);
        Console.Write(paddedLabel);
        Console.Write(": ");
        Console.Write(White);
        Console.Write(value);
        Console.Write(Reset);
        Console.WriteLine();
    }

    public static void Skip(string filename)
    {
        WriteTag(ColorInfo, "SKIP");
        Console.Write(White);
        Console.Write(filename);
        Console.Write(Reset);
        Console.WriteLine();
    }
}
```

---

### `LocalSendRenamer\DateValidator.cs`

```csharp
namespace LocalSendRenamer;

public static class DateValidator
{
    public const int MinPlausibleYear = 2000;
    public const int MaxPlausibleYear = 2030;

    public static bool AreAllPlausible(IEnumerable<FileEntry> entries, int minYear = MinPlausibleYear, int maxYear = MaxPlausibleYear)
    {
        bool allOk = true;

        foreach (FileEntry entry in entries)
        {
            DateTime date = entry.CaptureTime;

            if (date.Year >= minYear && date.Year <= maxYear)
            {
                continue;
            }

            ConsoleLogger.Error($"Implausible date {date:yyyy-MM-dd} for file: {entry.Path}");
            allOk = false;
        }

        return allOk;
    }
}
```

---

### `LocalSendRenamer\FileEntry.cs`

```csharp
namespace LocalSendRenamer;

public record FileEntry(string Path, DateTime CaptureTime);
```

---

### `LocalSendRenamer\FileNameGenerator.cs`

```csharp
using System.Globalization;

namespace LocalSendRenamer;

public class FileNameGenerator
{
    private static readonly PersianCalendar PersianCalendar = new();

    private readonly string _directoryPath;

    public FileNameGenerator(string directoryPath)
    {
        _directoryPath = directoryPath;
    }

    public IEnumerable<FileRenameInfo> Generate(string dateKey, IEnumerable<FileEntry> files)
    {
        List<FileEntry> sortedFiles = [.. files
            .OrderBy(x => x.CaptureTime)
            .ThenBy(x => Path.GetFileName(x.Path))];

        int totalFiles = sortedFiles.Count;

        for (int i = 0; i < totalFiles; i++)
        {
            yield return CreateRenameInfo(dateKey, i, totalFiles, sortedFiles[i]);
        }
    }

    private FileRenameInfo CreateRenameInfo(string dateKey, int index, int totalFiles, FileEntry file)
    {
        string baseName = totalFiles == 1 ?
            dateKey
            : $"{dateKey}-{index + 1}";

        string extension = Path.GetExtension(file.Path);
        string newPath = ResolveUniquePath(baseName, extension);

        return new(file.Path, newPath);
    }

    private string ResolveUniquePath(string baseName, string extension)
    {
        string targetPath = Path.Combine(_directoryPath, baseName + extension);

        if (!File.Exists(targetPath))
        {
            return targetPath;
        }

        int counter = 1;

        while (true)
        {
            string candidate = Path.Combine(_directoryPath, $"{baseName}-{counter}{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }

    public static string GetPersianDateString(DateTime time)
    {
        int year = PersianCalendar.GetYear(time);
        int month = PersianCalendar.GetMonth(time);
        int day = PersianCalendar.GetDayOfMonth(time);

        return $"{year:0000}-{month:00}-{day:00}";
    }

    public static string GetDateTimeKey(DateTime time)
    {
        return $"{GetPersianDateString(time)}_{time:HH-mm-ss}";
    }
}
```

---

### `LocalSendRenamer\FileRenameInfo.cs`

```csharp
namespace LocalSendRenamer;

public record FileRenameInfo(string OriginalPath, string NewPath);
```

---

### `LocalSendRenamer\FileScanner.cs`

```csharp
using System.Text.RegularExpressions;

namespace LocalSendRenamer;

public static partial class FileScanner
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}(?:_\d{2}-\d{2}-\d{2})?(?:-\d+)?$", RegexOptions.Compiled)]
    private static partial Regex AlreadyRenamedRegex();

    private static IEnumerable<string> GetDirectories(string baseDirectory)
    {
        return Directory.EnumerateDirectories(baseDirectory, "*", SearchOption.AllDirectories)
            .Prepend(baseDirectory);
    }

    private static bool IsAlreadyRenamed(string filePath)
    {
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        return AlreadyRenamedRegex().IsMatch(baseName);
    }

    public static (List<FileEntry> Entries, int SkippedCount) Scan(string baseDirectory, string[] allowedExtensions)
    {
        HashSet<string> allowedSet = [.. allowedExtensions.Select(e => e.ToLowerInvariant())];
        List<FileEntry> entries = [];
        int skippedCount = 0;

        foreach (string dir in GetDirectories(baseDirectory))
        {
            foreach (string file in Directory.EnumerateFiles(dir))
            {
                (FileEntry? entry, bool skipped) = ProcessFile(file, allowedSet);

                if (skipped)
                {
                    skippedCount++;
                }
                else if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
        }

        return (entries, skippedCount);
    }

    private static (FileEntry? Entry, bool Skipped) ProcessFile(string filePath, HashSet<string> allowedExtensions)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
        {
            return (null, false);
        }

        if (IsAlreadyRenamed(filePath))
        {
            ConsoleLogger.Skip(Path.GetFileName(filePath));
            return (null, true);
        }

        DateTime? date = CaptureDateExtractor.Extract(filePath);

        if (date.HasValue)
        {
            return (new(filePath, date.Value), false);
        }

        DateTime creation = File.GetCreationTime(filePath);
        DateTime modified = File.GetLastWriteTime(filePath);

        DateTime fallbackDate = creation < modified
            ? creation
            : modified;

        ConsoleLogger.Warning($"Using file system date ({fallbackDate:yyyy-MM-dd}) as capture date for: {Path.GetFileName(filePath)}");

        return (new(filePath, fallbackDate), false);
    }
}
```

---

### `LocalSendRenamer\LocalSendRenamer.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<OutputType>Exe</OutputType>
		<TargetFramework>net10.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>

	<PropertyGroup>
		<OutputPath>..\artifacts\$(Configuration)\</OutputPath>
		<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
		<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="MetadataExtractor" Version="2.9.3" />
		<PackageReference Include="System.CommandLine" Version="2.0.10" />
	</ItemGroup>

</Project>
```

---

### `LocalSendRenamer\ManifestManager.cs`

```csharp
using System.Text.Json;

namespace LocalSendRenamer;

public record RenameManifestEntry(string OriginalPath, string NewPath);

public static class ManifestManager
{
    private const string ManifestFileName = ".rename_manifest.json";
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public static void Save(string directory, IEnumerable<FileRenameInfo> renames)
    {
        List<RenameManifestEntry> entries = [.. renames.Select(r => new RenameManifestEntry(r.OriginalPath, r.NewPath))];
        string manifestPath = Path.Combine(directory, ManifestFileName);
        string json = JsonSerializer.Serialize(entries, _jsonOptions);

        File.WriteAllText(manifestPath, json);
    }

    public static List<RenameManifestEntry> Load(string directory)
    {
        string manifestPath = Path.Combine(directory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return [];
        }

        string json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<List<RenameManifestEntry>>(json) ?? [];
    }

    public static void Delete(string directory)
    {
        string manifestPath = Path.Combine(directory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return;
        }

        File.Delete(manifestPath);
    }
}
```

---

### `LocalSendRenamer\PathInstaller.cs`

```csharp
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

        return [.. currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries)];
    }

    private static void SetEnvironmentPaths(List<string> paths)
    {
        string newPath = string.Join(';', paths);
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
    }
}
```

---

### `LocalSendRenamer\Program.cs`

```csharp
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
```

---

### `LocalSendRenamer\RenameBatch.cs`

```csharp
namespace LocalSendRenamer;

public record RenameBatch(string Directory, IReadOnlyList<FileRenameInfo> Renames);
```

---

### `LocalSendRenamer\RenameExecutor.cs`

```csharp
namespace LocalSendRenamer;

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
```

---

### `LocalSendRenamer\RenameOrchestrator.cs`

```csharp
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

        (List<FileEntry>? entries, int skippedCount) = FileScanner.Scan(baseDirectory, AllowedExtensions);
        int totalScanned = skippedCount + entries.Count;

        if (!DateValidator.AreAllPlausible(entries))
        {
            ConsoleLogger.Error("\nAborting: no files were renamed.");
            Console.ReadLine();
            return;
        }

        IEnumerable<RenameBatch> batches = new RenamePlanner(baseDirectory, AllowedExtensions).Plan(entries);
        (int totalRenamed, List<FileRenameInfo>? allRenames) = ExecuteRenames(batches, dryRun);

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

        ConsoleLogger.AlignedInfo("Total", totalScanned.ToString());
        ConsoleLogger.AlignedInfo("Skipped", skippedCount.ToString());
        ConsoleLogger.Success("Renamed", totalRenamed.ToString());
        ConsoleLogger.Success("Time", elapsed.ToString());

        if (!dryRun)
        {
            return;
        }

        ConsoleLogger.Info("\nDry run complete – no files were actually changed.");
    }
}
```

---

### `LocalSendRenamer\RenamerPlanner.cs`

```csharp
namespace LocalSendRenamer;

public class RenamePlanner
{
    private readonly string _baseDirectory;
    private readonly HashSet<string> _allowedExtensions;

    public RenamePlanner(string baseDirectory, IEnumerable<string> allowedExtensions)
    {
        _baseDirectory = baseDirectory;
        _allowedExtensions = [.. allowedExtensions.Select(e => e.ToLowerInvariant())];
    }

    public IEnumerable<RenameBatch> Plan(IEnumerable<FileEntry> entries)
    {
        List<FileEntry> validEntries = GetValidEntries(entries);

        if (validEntries.Count == 0)
        {
            return [];
        }

        IEnumerable<string> directories = GetDirectories(validEntries);
        List<RenameBatch> batches = [];

        foreach (string directory in directories)
        {
            List<FileEntry> directoryFiles = GetDirectoryFiles(validEntries, directory);
            List<IGrouping<string, FileEntry>> groups = GetGroups(directoryFiles);

            foreach (IGrouping<string, FileEntry> group in groups)
            {
                FileNameGenerator generator = new(directory);
                List<FileRenameInfo> renames = [.. generator.Generate(group.Key, group)];

                batches.Add(new(directory, renames));
            }
        }

        return batches;
    }

    private static List<FileEntry> GetDirectoryFiles(List<FileEntry> validEntries, string directory)
    {
        return [.. validEntries.Where(e => Path.GetDirectoryName(e.Path) == directory)];
    }

    private static List<IGrouping<string, FileEntry>> GetGroups(List<FileEntry> directoryFiles)
    {
        return [.. directoryFiles.GroupBy(e => FileNameGenerator.GetDateTimeKey(e.CaptureTime))];
    }

    private static IEnumerable<string> GetDirectories(List<FileEntry> validEntries)
    {
        return validEntries
            .Select(e => Path.GetDirectoryName(e.Path) ?? string.Empty)
            .Distinct();
    }

    private List<FileEntry> GetValidEntries(IEnumerable<FileEntry> entries)
    {
        return [.. entries
            .Where(e => _allowedExtensions.Contains(Path.GetExtension(e.Path).ToLowerInvariant()))
            .Where(e => e.CaptureTime != default)];
    }
}
```

---

### `LocalSendRenamer\UndoCommand.cs`

```csharp

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
```

---

### `LocalSendRenamer.Tests\CaptureDateExtractorTests.cs`

```csharp
using FluentAssertions;
using Xunit;

namespace LocalSendRenamer.Tests;

public class CaptureDateExtractorTests
{
    [Theory]
    [InlineData("IMG_20231005_123045.jpg", 2023, 10, 5, 12, 30, 45, 0)]
    [InlineData("VID_20220101_000000.mp4", 2022, 1, 1, 0, 0, 0, 0)]
    [InlineData("Screenshot_2023-11-20-15-45-10-123.png", 2023, 11, 20, 15, 45, 10, 123)]
    public void Extract_FilenamePatterns_ReturnsParsedDate(string fileName, int year, int month, int day, int h, int m, int s, int ms)
    {
        DateTime? result = CaptureDateExtractor.Extract(fileName);

        result.Should().NotBeNull();
        result!.Value.Should().Be(new(year, month, day, h, m, s, ms));
    }

    [Theory]
    [InlineData("img_20231005_123045.jpg")]
    [InlineData("Img_20231005_123045.jpg")]
    [InlineData("IMG_20231005_123045.jpg")]
    [InlineData("screenshot_2023-11-20-15-45-10-123.png")]
    [InlineData("ScreenRecorder_2023-11-20-15-45-10-123.png")]
    public void Extract_CaseInsensitivePatterns_ReturnsParsedDate(string fileName)
    {
        DateTime? result = CaptureDateExtractor.Extract(fileName);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Extract_InvalidPattern_ReturnsNull()
    {
        string fileName = "RandomFile.txt";

        DateTime? result = CaptureDateExtractor.Extract(fileName);

        result.Should().BeNull();
    }

    [Fact]
    public void Extract_NonExistentFileWithNoMetadata_ReturnsNull()
    {
        string fileName = "NonExistentFile.jpg";

        DateTime? result = CaptureDateExtractor.Extract(fileName);

        result.Should().BeNull();
    }
}
```

---

### `LocalSendRenamer.Tests\DateValidatorTests.cs`

```csharp
using FluentAssertions;
using Xunit;

namespace LocalSendRenamer.Tests;

public class DateValidatorTests
{
    [Fact]
    public void AreAllPlausible_AllDatesWithinRange_ReturnsTrue()
    {
        List<FileEntry> entries =
        [
            new("file1.jpg", new(2025, 1, 1)),
            new("file2.jpg", new(2030, 12, 31)),
            new("file3.jpg", new(2000, 6, 15))
        ];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeTrue();
    }

    [Fact]
    public void AreAllPlausible_WithDateBeforeMin_ReturnsFalse()
    {
        List<FileEntry> entries =
        [
            new("file1.jpg", new(2025, 1, 1)),
            new("file2.jpg", new(1999, 12, 31))
        ];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeFalse();
    }

    [Fact]
    public void AreAllPlausible_WithDateAfterMax_ReturnsFalse()
    {
        List<FileEntry> entries =
        [
            new("file1.jpg", new(2031, 1, 1))
        ];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeFalse();
    }

    [Fact]
    public void AreAllPlausible_EmptyEntries_ReturnsTrue()
    {
        List<FileEntry> entries = [];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeTrue();
    }

    [Fact]
    public void AreAllPlausible_AtExactBoundaries_ReturnsTrue()
    {
        List<FileEntry> entries =
        [
            new("min.jpg", new(2000, 1, 1)),
            new("max.jpg", new(2030, 12, 31, 23, 59, 59))
        ];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeTrue();
    }
}
```

---

### `LocalSendRenamer.Tests\FileNameGeneratorTests.cs`

```csharp
using FluentAssertions;
using Xunit;

namespace LocalSendRenamer.Tests;

public class FileNameGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public FileNameGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Generate_SingleFileUsesDateTimeKey()
    {
        FileEntry[] files = [new("test.jpg", new(2023, 3, 21, 14, 30, 45))];
        string dateTimeKey = "1402-01-01_14-30-45";

        FileNameGenerator generator = new("C:\\");
        List<FileRenameInfo> result = [.. generator.Generate(dateTimeKey, files)];

        result.Should().HaveCount(1);
        result[0].NewPath.Should().EndWith("1402-01-01_14-30-45.jpg");
    }

    [Fact]
    public void Generate_MultipleFilesSameSecond_AppendsCounter()
    {
        // Both files at same second (14:30:45)
        FileEntry[] files =
        [
            new("A.jpg", new(2023, 3, 21, 14, 30, 45)),
            new("B.jpg", new(2023, 3, 21, 14, 30, 45))
        ];

        string dateTimeKey = "1402-01-01_14-30-45";

        FileNameGenerator generator = new("C:\\");
        List<FileRenameInfo> result = [.. generator.Generate(dateTimeKey, files)];

        result.Should().HaveCount(2);
        result[0].NewPath.Should().EndWith("1402-01-01_14-30-45-1.jpg");
        result[1].NewPath.Should().EndWith("1402-01-01_14-30-45-2.jpg");
    }

    [Fact]
    public void Generate_FileExists_AppendsCounter()
    {
        string dateTimeKey = "1402-01-01_14-30-45";
        File.WriteAllText(Path.Combine(_tempDir, dateTimeKey + ".jpg"), "existing");
        FileEntry[] files = [new("test.jpg", new(2023, 3, 21, 14, 30, 45))];

        FileNameGenerator generator = new(_tempDir);
        List<FileRenameInfo> result = [.. generator.Generate(dateTimeKey, files)];

        result.Should().HaveCount(1);
        result[0].NewPath.Should().EndWith(dateTimeKey + "-1.jpg");
    }

    [Fact]
    public void Generate_MultipleCollisions_FindsNextCounter()
    {
        string dateTimeKey = "1402-01-01_14-30-45";
        File.WriteAllText(Path.Combine(_tempDir, dateTimeKey + ".jpg"), "existing1");
        File.WriteAllText(Path.Combine(_tempDir, dateTimeKey + "-1.jpg"), "existing2");
        File.WriteAllText(Path.Combine(_tempDir, dateTimeKey + "-2.jpg"), "existing3");

        FileEntry[] files = [new("test.jpg", new(2023, 3, 21, 14, 30, 45))];

        FileNameGenerator generator = new(_tempDir);
        List<FileRenameInfo> result = [.. generator.Generate(dateTimeKey, files)];

        result.Should().HaveCount(1);
        result[0].NewPath.Should().EndWith(dateTimeKey + "-3.jpg");
    }
}
```

---

### `LocalSendRenamer.Tests\FileScannerFallbackTests.cs`

```csharp
using FluentAssertions;
using Xunit;

namespace LocalSendRenamer.Tests;

public class FileScannerFallbackTests : IDisposable
{
    private readonly string _tempDir;

    public FileScannerFallbackTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Scan_WhenMetadataMissing_UsesFileSystemDate()
    {
        // Create a file with no metadata (plain text file)
        string filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "dummy content");

        // Set creation time older than last write
        DateTime creationTime = new(2025, 5, 1, 10, 0, 0);
        DateTime lastWriteTime = new(2026, 4, 30, 15, 30, 0);
        File.SetCreationTime(filePath, creationTime);
        File.SetLastWriteTime(filePath, lastWriteTime);

        // Scan with allowed extensions including .txt (temporarily)
        string[] allowedExtensions = [".txt"];
        (List<FileEntry>? entries, int skipped) = FileScanner.Scan(_tempDir, allowedExtensions);

        skipped.Should().Be(0);
        entries.Should().HaveCount(1);
        entries[0].Path.Should().Be(filePath);
        // Should use older date (creation)
        entries[0].CaptureTime.Should().Be(creationTime);
    }

    [Fact]
    public void Scan_WhenLastWriteIsOlder_UsesLastWrite()
    {
        string filePath = Path.Combine(_tempDir, "test2.txt");
        File.WriteAllText(filePath, "dummy");

        DateTime creationTime = new(2026, 5, 3, 12, 0, 0);
        DateTime lastWriteTime = new(2025, 10, 1, 8, 0, 0);
        File.SetCreationTime(filePath, creationTime);
        File.SetLastWriteTime(filePath, lastWriteTime);

        string[] allowedExtensions = [".txt"];
        (List<FileEntry>? entries, int _) = FileScanner.Scan(_tempDir, allowedExtensions);

        entries.Should().HaveCount(1);
        entries[0].CaptureTime.Should().Be(lastWriteTime);
    }

    [Fact]
    public void Scan_WhenFileAlreadyRenamed_SkipsAndCounts()
    {
        // Create file with Persian date pattern
        string fileName = "1405-02-12-1.txt";
        string filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, "dummy");

        string[] allowedExtensions = [".txt"];
        (List<FileEntry>? entries, int skipped) = FileScanner.Scan(_tempDir, allowedExtensions);

        skipped.Should().Be(1);
        entries.Should().BeEmpty();
    }
}
```

---

### `LocalSendRenamer.Tests\LocalSendRenamer.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
		<IsPackable>false</IsPackable>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="FluentAssertions" Version="8.10.0" />
		<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
		<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
		  <PrivateAssets>all</PrivateAssets>
		  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
		</PackageReference>
		<PackageReference Include="xunit.v3" Version="3.2.2" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\LocalSendRenamer\LocalSendRenamer.csproj" />
	</ItemGroup>

</Project>
```

---

### `LocalSendRenamer.Tests\ManifestManagerTests.cs`

```csharp
using FluentAssertions;
using Xunit;

namespace LocalSendRenamer.Tests;

public class ManifestManagerTests : IDisposable
{
    private readonly string _tempDir;

    public ManifestManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Save_ShouldCreateManifestFile()
    {
        List<FileRenameInfo> renames =
        [
            new(@"C:\test\old1.jpg", @"C:\test\new1.jpg"),
            new(@"C:\test\old2.mp4", @"C:\test\new2.mp4")
        ];

        ManifestManager.Save(_tempDir, renames);

        string manifestPath = Path.Combine(_tempDir, ".rename_manifest.json");
        File.Exists(manifestPath).Should().BeTrue();

        string content = File.ReadAllText(manifestPath);
        content.Should().Contain("old1.jpg");
        content.Should().Contain("new1.jpg");
        content.Should().Contain("old2.mp4");
        content.Should().Contain("new2.mp4");
    }

    [Fact]
    public void Load_WhenManifestExists_ReturnsEntries()
    {
        List<RenameManifestEntry> originalEntries =
        [
            new(@"C:\test\old1.jpg", @"C:\test\new1.jpg"),
            new(@"C:\test\old2.mp4", @"C:\test\new2.mp4")
        ];
        string manifestPath = Path.Combine(_tempDir, ".rename_manifest.json");
        string json = System.Text.Json.JsonSerializer.Serialize(originalEntries);
        File.WriteAllText(manifestPath, json);

        List<RenameManifestEntry> loaded = ManifestManager.Load(_tempDir);

        loaded.Should().HaveCount(2);
        loaded[0].OriginalPath.Should().Be(@"C:\test\old1.jpg");
        loaded[0].NewPath.Should().Be(@"C:\test\new1.jpg");
        loaded[1].OriginalPath.Should().Be(@"C:\test\old2.mp4");
        loaded[1].NewPath.Should().Be(@"C:\test\new2.mp4");
    }

    [Fact]
    public void Load_WhenNoManifest_ReturnsEmptyList()
    {
        List<RenameManifestEntry> loaded = ManifestManager.Load(_tempDir);
        loaded.Should().BeEmpty();
    }

    [Fact]
    public void Delete_WhenManifestExists_RemovesManifestFile()
    {
        List<FileRenameInfo> renames =
        [
            new(@"C:\test\old.jpg", @"C:\test\new.jpg")
        ];
        ManifestManager.Save(_tempDir, renames);
        string manifestPath = Path.Combine(_tempDir, ".rename_manifest.json");
        File.Exists(manifestPath).Should().BeTrue();

        ManifestManager.Delete(_tempDir);
        File.Exists(manifestPath).Should().BeFalse();
    }

    [Fact]
    public void Delete_WhenNoManifest_DoesNothing()
    {
        Action act = () => ManifestManager.Delete(_tempDir);
        act.Should().NotThrow();
    }
}
```

---

### `LocalSendRenamer.Tests\RenamePlannerTests.cs`

```csharp
using FluentAssertions;
using Xunit;

namespace LocalSendRenamer.Tests;

public class RenamePlannerTests
{
    [Fact]
    public void Plan_EmptyEntries_ReturnsEmpty()
    {
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Plan_SingleFile_ReturnsSingleBatch()
    {
        FileEntry[] entries = [new("C:\\Photos\\test.jpg", new(2023, 3, 21))];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        result.Should().HaveCount(1);
        result[0].Directory.Should().Be("C:\\Photos");
        result[0].Renames.Should().HaveCount(1);
    }

    [Fact]
    public void Plan_MultipleDirectories_GroupsByDirectory()
    {
        FileEntry[] entries =
        [
            new("C:\\Photos\\a.jpg", new(2023, 3, 21)),
            new("C:\\Videos\\b.jpg", new(2023, 3, 21))
        ];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Directory == "C:\\Photos");
        result.Should().Contain(b => b.Directory == "C:\\Videos");
    }

    [Fact]
    public void Plan_DifferentTimesSameDate_SeparateBatches()
    {
        FileEntry[] entries =
        [
            new("C:\\Photos\\morning.jpg", new(2023, 3, 21, 8, 0, 0)),
            new("C:\\Photos\\evening.jpg", new(2023, 3, 21, 18, 0, 0))
        ];

        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        // Each different time yields separate batch
        result.Should().HaveCount(2);
        result[0].Renames.Should().HaveCount(1);
        result[1].Renames.Should().HaveCount(1);
    }

    [Fact]
    public void Plan_SameSecond_SingleBatchWithCounter()
    {
        FileEntry[] entries =
        [
            new("C:\\Photos\\first.jpg", new(2023, 3, 21, 14, 30, 45)),
            new("C:\\Photos\\second.jpg", new(2023, 3, 21, 14, 30, 45))
        ];

        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        result.Should().HaveCount(1);
        result[0].Renames.Should().HaveCount(2);
    }

    [Fact]
    public void Plan_DifferentDates_SeparateBatches()
    {
        FileEntry[] entries =
        [
            new("C:\\Photos\\day1.jpg", new(2023, 3, 21)),
            new("C:\\Photos\\day2.jpg", new(2023, 3, 22))
        ];

        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Plan_InvalidExtension_Excluded()
    {
        FileEntry[] entries = [new("C:\\Photos\\test.txt", new(2023, 3, 21))];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan(entries);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Plan_DefaultDate_Excluded()
    {
        FileEntry[] entries = [new("C:\\Photos\\test.jpg", default)];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan(entries);

        result.Should().BeEmpty();
    }
}
```

---

### `LocalSendRenamer.Tests\UndoCommandTests.cs`

```csharp
using FluentAssertions;
using Xunit;

namespace LocalSendRenamer.Tests;

public class UndoCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TextWriter _originalConsoleOut;

    public UndoCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(_tempDir);

        _originalConsoleOut = Console.Out;
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOut);

        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    private static StringWriter CaptureConsole(out StringWriter writer)
    {
        writer = new();
        Console.SetOut(writer);
        return writer;
    }

    [Fact]
    public void Undo_WhenNoManifest_ShowsWarning()
    {
        using (CaptureConsole(out StringWriter? writer))
        {
            UndoCommand.Run(_tempDir);
            string output = writer.ToString();
            output.Should().Contain("No manifest found");
        }
    }

    [Fact]
    public void Undo_WithSingleFile_RevertsAndDeletesManifest()
    {
        string originalPath = Path.Combine(_tempDir, "original.jpg");
        string newPath = Path.Combine(_tempDir, "1405-02-12.jpg");
        File.WriteAllText(originalPath, "content");
        File.Move(originalPath, newPath);

        List<FileRenameInfo> renames = [new(originalPath, newPath)];
        ManifestManager.Save(_tempDir, renames);

        UndoCommand.Run(_tempDir);

        File.Exists(newPath).Should().BeFalse();
        File.Exists(originalPath).Should().BeTrue();
        File.ReadAllText(originalPath).Should().Be("content");
        ManifestManager.Load(_tempDir).Should().BeEmpty();
    }

    [Fact]
    public void Undo_WithMultipleFiles_RevertsAllAndDeletesManifest()
    {
        List<(string orig, string newf)> entries = [];
        List<FileRenameInfo> renames = [];

        for (int i = 1; i <= 3; i++)
        {
            string orig = Path.Combine(_tempDir, $"original{i}.txt");
            string newf = Path.Combine(_tempDir, $"1405-02-12-{i}.txt");
            File.WriteAllText(orig, $"content{i}");
            File.Move(orig, newf);
            entries.Add((orig, newf));
            renames.Add(new(orig, newf));
        }
        ManifestManager.Save(_tempDir, renames);

        UndoCommand.Run(_tempDir);

        foreach ((string? orig, string? newf) in entries)
        {
            File.Exists(newf).Should().BeFalse();
            File.Exists(orig).Should().BeTrue();
            File.ReadAllText(orig).Should().MatchRegex("content\\d");
        }
        ManifestManager.Load(_tempDir).Should().BeEmpty();
    }

    [Fact]
    public void Undo_WhenNewFileMissing_WarnsAndSkips()
    {
        string originalPath = Path.Combine(_tempDir, "original.jpg");
        string newPath = Path.Combine(_tempDir, "1405-02-12.jpg");
        List<FileRenameInfo> renames = [new(originalPath, newPath)];
        ManifestManager.Save(_tempDir, renames);

        using (CaptureConsole(out StringWriter? writer))
        {
            UndoCommand.Run(_tempDir);
            writer.ToString().Should().Contain("New file missing, cannot revert");
        }
        ManifestManager.Load(_tempDir).Should().NotBeEmpty();
    }

    [Fact]
    public void Undo_WhenOriginalAlreadyExists_WarnsAndSkips()
    {
        string originalPath = Path.Combine(_tempDir, "original.jpg");
        string newPath = Path.Combine(_tempDir, "1405-02-12.jpg");
        File.WriteAllText(originalPath, "original content");
        File.WriteAllText(newPath, "new content");
        List<FileRenameInfo> renames = [new(originalPath, newPath)];
        ManifestManager.Save(_tempDir, renames);

        using (CaptureConsole(out StringWriter? writer))
        {
            UndoCommand.Run(_tempDir);
            writer.ToString().Should().Contain("Original already exists, skipping revert");
        }

        ManifestManager.Load(_tempDir).Should().NotBeEmpty();
    }

    [Fact]
    public void Undo_WithPartialFailure_PreservesManifest()
    {
        string orig1 = Path.Combine(_tempDir, "good.jpg");
        string new1 = Path.Combine(_tempDir, "good-renamed.jpg");
        File.WriteAllText(orig1, "good");
        File.Move(orig1, new1);

        string orig2 = Path.Combine(_tempDir, "conflict.jpg");
        string new2 = Path.Combine(_tempDir, "conflict-renamed.jpg");
        File.WriteAllText(orig2, "original already here");
        File.WriteAllText(new2, "conflict new content");

        List<FileRenameInfo> renames = [new(orig1, new1), new(orig2, new2)];
        ManifestManager.Save(_tempDir, renames);

        using (CaptureConsole(out StringWriter? writer))
        {
            UndoCommand.Run(_tempDir);
            string output = writer.ToString();
            output.Should().Contain("Original already exists, skipping revert");
            output.Should().Contain("Partially undone: 1 of 2 files");
        }

        File.Exists(new1).Should().BeFalse();
        File.Exists(orig1).Should().BeTrue();
        File.Exists(new2).Should().BeTrue();
        File.Exists(orig2).Should().BeTrue();
        ManifestManager.Load(_tempDir).Should().HaveCount(2);
    }
}
```

---

