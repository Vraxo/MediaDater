using AwesomeAssertions;
using Xunit;

namespace MediaDater.Tests;

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