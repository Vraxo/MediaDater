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
        return GetFilenameDate(filePath)
            ?? GetMetadataDate(filePath);
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
        if (!IsDateValid(input, format, out DateTime result))
        {
            return null;
        }

        return result;
    }

    private static bool IsDateValid(string input, string format, out DateTime result)
    {
        return DateTime.TryParseExact(
            input,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
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
        ExifSubIfdDirectory? subIfd = GetSubIfd(directories);

        if (subIfd?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime date) == true)
        {
            return date;
        }

        ExifIfd0Directory? ifd0 = GetIfd0(directories);

        if (ifd0?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out date) == true)
        {
            return date;
        }

        return null;
    }

    private static ExifSubIfdDirectory? GetSubIfd(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        return directories
            .OfType<ExifSubIfdDirectory>()
            .FirstOrDefault();
    }

    private static ExifIfd0Directory? GetIfd0(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        return directories
            .OfType<ExifIfd0Directory>()
            .FirstOrDefault();
    }

    private static DateTime? GetQuickTimeDate(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        QuickTimeMovieHeaderDirectory? movieHeader = GetMovieHeader(directories);

        if (DoesMetadataExist(movieHeader, out DateTime date))
        {
            return null;
        }

        return DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime();
    }

    private static bool DoesMetadataExist(QuickTimeMovieHeaderDirectory? movieHeader, out DateTime date)
    {
        date = default;
        return movieHeader?.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out date) != true;
    }

    private static QuickTimeMovieHeaderDirectory? GetMovieHeader(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        return directories
            .OfType<QuickTimeMovieHeaderDirectory>()
            .FirstOrDefault();
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
        string baseName = totalFiles == 1 ? dateKey : $"{dateKey}-{index + 1}";
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
            string candidate = Path.Combine(_directoryPath, $"{baseName}_{counter}{extension}");

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
}
```

---

### `LocalSendRenamer\FileRenameInfo.cs`

```csharp
namespace LocalSendRenamer;

public record FileRenameInfo(string OriginalPath, string NewPath);
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
    <PackageReference Include="MetadataExtractor" Version="2.9.0-rc2" />
  </ItemGroup>

</Project>
```

---

### `LocalSendRenamer\Program.cs`

```csharp
using System.Diagnostics;

namespace LocalSendRenamer;

internal class Program
{
    private const string BaseDirectory = @"C:\Users\Parsa\Downloads\LocalSend";

    private static readonly string[] AllowedExtensions =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
        ".mp4",
        ".jfif"
    ];

    private static void Main()
    {
        Console.WriteLine("Starting rename process...\n");

        Stopwatch stopwatch = Stopwatch.StartNew();
        IEnumerable<FileEntry> entries = CollectFileEntries(BaseDirectory, AllowedExtensions);
        RenamePlanner planner = new(BaseDirectory, AllowedExtensions);
        IEnumerable<RenameBatch> batches = planner.Plan(entries);

        int totalRenamed = 0;

        foreach (RenameBatch batch in batches)
        {
            foreach (FileRenameInfo rename in batch.Renames)
            {
                ExecuteRename(rename);
                totalRenamed++;
            }
        }

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"Total Renamed: {totalRenamed}");
        Console.WriteLine($"Execution Time: {stopwatch.Elapsed}");
        Console.ReadLine();
    }

    private static IEnumerable<FileEntry> CollectFileEntries(string baseDirectory, string[] allowedExtensions)
    {
        HashSet<string> allowedSet = [.. allowedExtensions.Select(e => e.ToLowerInvariant())];

        foreach (string? dir in GetDirectories(baseDirectory))
        {
            foreach (string file in Directory.EnumerateFiles(dir))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();

                if (!allowedSet.Contains(ext))
                {
                    continue;
                }

                DateTime? date = CaptureDateExtractor.Extract(file);

                if (!date.HasValue)
                {
                    continue;
                }

                yield return new(file, date.Value);
            }
        }
    }

    private static IEnumerable<string> GetDirectories(string baseDirectory)
    {
        return Directory.EnumerateDirectories(baseDirectory, "*", SearchOption.AllDirectories)
            .Prepend(baseDirectory);
    }

    private static void ExecuteRename(FileRenameInfo rename)
    {
        if (PathsAreSame(rename))
        {
            return;
        }

        string originalName = Path.GetFileName(rename.OriginalPath);

        try
        {
            File.Move(rename.OriginalPath, rename.NewPath);

            string newName = Path.GetFileName(rename.NewPath);

            Console.WriteLine($"[OK] {originalName} -> {newName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERR] {originalName}: {ex.Message}");
        }
    }

    private static bool PathsAreSame(FileRenameInfo rename)
    {
        string originalPath = Path.GetFullPath(rename.OriginalPath);
        string newPath = Path.GetFullPath(rename.NewPath);

        return originalPath.Equals(newPath, StringComparison.OrdinalIgnoreCase);
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
        return [.. directoryFiles.GroupBy(e => FileNameGenerator.GetPersianDateString(e.CaptureTime))];
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

### `LocalSendRenamer.Tests\CaptureDateExtractorTests.cs`

```csharp
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

        Assert.NotNull(result);
        Assert.Equal(new DateTime(year, month, day, h, m, s, ms), result!.Value);
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

        Assert.NotNull(result);
    }

    [Fact]
    public void Extract_InvalidPattern_ReturnsNull()
    {
        string fileName = "RandomFile.txt";

        DateTime? result = CaptureDateExtractor.Extract(fileName);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_NonExistentFileWithNoMetadata_ReturnsNull()
    {
        string fileName = "NonExistentFile.jpg";

        DateTime? result = CaptureDateExtractor.Extract(fileName);

        Assert.Null(result);
    }
}
```

---

### `LocalSendRenamer.Tests\FileNameGeneratorTests.cs`

```csharp
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
    public void Generate_SingleFile_UsesDateOnly()
    {
        FileEntry[] files = [new("test.jpg", new DateTime(2023, 3, 21))];
        string dateKey = "1402-01-01";

        FileNameGenerator generator = new("C:\\");
        List<FileRenameInfo> result = [.. generator.Generate(dateKey, files)];

        Assert.Single(result);
        Assert.EndsWith("1402-01-01.jpg", result[0].NewPath);
    }

    [Fact]
    public void Generate_MultipleFiles_AppendsCounter()
    {
        FileEntry[] files =
        [
            new("A.jpg", new DateTime(2023, 3, 21, 10, 0, 0)),
            new("B.jpg", new DateTime(2023, 3, 21, 11, 0, 0))
        ];

        string dateKey = "1402-01-01";

        FileNameGenerator generator = new("C:\\");
        List<FileRenameInfo> result = [.. generator.Generate(dateKey, files)];

        Assert.Equal(2, result.Count);
        Assert.EndsWith("1402-01-01-1.jpg", result[0].NewPath);
        Assert.EndsWith("1402-01-01-2.jpg", result[1].NewPath);
    }

    [Fact]
    public void Generate_FileExists_AppendsCounter()
    {
        File.WriteAllText(Path.Combine(_tempDir, "1402-01-01.jpg"), "existing");
        FileEntry[] files = [new("test.jpg", new DateTime(2023, 3, 21))];
        string dateKey = "1402-01-01";

        FileNameGenerator generator = new(_tempDir);
        List<FileRenameInfo> result = [.. generator.Generate(dateKey, files)];

        Assert.Single(result);
        Assert.EndsWith("1402-01-01_1.jpg", result[0].NewPath);
    }

    [Fact]
    public void Generate_MultipleCollisions_IncrementCounter()
    {
        File.WriteAllText(Path.Combine(_tempDir, "1402-01-01.jpg"), "existing1");
        File.WriteAllText(Path.Combine(_tempDir, "1402-01-01_1.jpg"), "existing2");
        File.WriteAllText(Path.Combine(_tempDir, "1402-01-01_2.jpg"), "existing3");
        FileEntry[] files = [new("test.jpg", new DateTime(2023, 3, 21))];
        string dateKey = "1402-01-01";

        FileNameGenerator generator = new(_tempDir);
        List<FileRenameInfo> result = [.. generator.Generate(dateKey, files)];

        Assert.Single(result);
        Assert.EndsWith("1402-01-01_3.jpg", result[0].NewPath);
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
		<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
		<PackageReference Include="xunit" Version="2.6.2" />
		<PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\LocalSendRenamer\LocalSendRenamer.csproj" />
	</ItemGroup>

</Project>
```

---

### `LocalSendRenamer.Tests\RenamePlannerTests.cs`

```csharp
using Xunit;

namespace LocalSendRenamer.Tests;

public class RenamePlannerTests
{
    [Fact]
    public void Plan_EmptyEntries_ReturnsEmpty()
    {
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan([]);

        Assert.Empty(result);
    }

    [Fact]
    public void Plan_SingleFile_ReturnsSingleBatch()
    {
        FileEntry[] entries = [new FileEntry("C:\\Photos\\test.jpg", new(2023, 3, 21))];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        Assert.Single(result);
        Assert.Equal("C:\\Photos", result[0].Directory);
        Assert.Single(result[0].Renames);
    }

    [Fact]
    public void Plan_MultipleDirectories_GroupsByDirectory()
    {
        FileEntry[] entries = new[]
        {
            new FileEntry("C:\\Photos\\a.jpg", new DateTime(2023, 3, 21)),
            new FileEntry("C:\\Videos\\b.jpg", new DateTime(2023, 3, 21))
        };
        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = planner.Plan(entries).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.Directory == "C:\\Photos");
        Assert.Contains(result, b => b.Directory == "C:\\Videos");
    }

    [Fact]
    public void Plan_SameDateDifferentFiles_GroupsByDate()
    {
        FileEntry[] entries =
        [
            new FileEntry("C:\\Photos\\morning.jpg", new(2023, 3, 21, 8, 0, 0)),
            new FileEntry("C:\\Photos\\evening.jpg", new(2023, 3, 21, 18, 0, 0))
        ];

        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = planner.Plan(entries).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Renames.Count);
    }

    [Fact]
    public void Plan_DifferentDates_SeparateBatches()
    {
        FileEntry[] entries =
        [
            new FileEntry("C:\\Photos\\day1.jpg", new(2023, 3, 21)),
            new FileEntry("C:\\Photos\\day2.jpg", new(2023, 3, 22))
        ];

        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = planner.Plan(entries).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Plan_InvalidExtension_Excluded()
    {
        FileEntry[] entries = [new FileEntry("C:\\Photos\\test.txt", new(2023, 3, 21))];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan(entries);

        Assert.Empty(result);
    }

    [Fact]
    public void Plan_DefaultDate_Excluded()
    {
        FileEntry[] entries = [new FileEntry("C:\\Photos\\test.jpg", default)];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan(entries);

        Assert.Empty(result);
    }
}
```

---

