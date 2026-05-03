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
    public void Generate_SingleFile_UsesDateOnly()
    {
        FileEntry[] files = [new("test.jpg", new(2023, 3, 21))];
        string dateKey = "1402-01-01";

        FileNameGenerator generator = new("C:\\");
        List<FileRenameInfo> result = [.. generator.Generate(dateKey, files)];

        result.Should().HaveCount(1);
        result[0].NewPath.Should().EndWith("1402-01-01.jpg");
    }

    [Fact]
    public void Generate_MultipleFiles_AppendsCounter()
    {
        FileEntry[] files =
        [
            new("A.jpg", new(2023, 3, 21, 10, 0, 0)),
            new("B.jpg", new(2023, 3, 21, 11, 0, 0))
        ];

        string dateKey = "1402-01-01";

        FileNameGenerator generator = new("C:\\");
        List<FileRenameInfo> result = [.. generator.Generate(dateKey, files)];

        result.Should().HaveCount(2);
        result[0].NewPath.Should().EndWith("1402-01-01-1.jpg");
        result[1].NewPath.Should().EndWith("1402-01-01-2.jpg");
    }

    [Fact]
    public void Generate_FileExists_AppendsCounter()
    {
        File.WriteAllText(Path.Combine(_tempDir, "1402-01-01.jpg"), "existing");
        FileEntry[] files = [new("test.jpg", new(2023, 3, 21))];
        string dateKey = "1402-01-01";

        FileNameGenerator generator = new(_tempDir);
        List<FileRenameInfo> result = [.. generator.Generate(dateKey, files)];

        result.Should().HaveCount(1);
        result[0].NewPath.Should().EndWith("1402-01-01-1.jpg");
    }

    [Fact]
    public void Generate_MultipleCollisions_FindsNextCounter()
    {
        File.WriteAllText(Path.Combine(_tempDir, "1402-01-01.jpg"), "existing1");
        File.WriteAllText(Path.Combine(_tempDir, "1402-01-01-1.jpg"), "existing2");
        File.WriteAllText(Path.Combine(_tempDir, "1402-01-01-2.jpg"), "existing3");

        FileEntry[] files = [new("test.jpg", new(2023, 3, 21))];
        string dateKey = "1402-01-01";

        FileNameGenerator generator = new(_tempDir);
        List<FileRenameInfo> result = [.. generator.Generate(dateKey, files)];

        result.Should().HaveCount(1);
        result[0].NewPath.Should().EndWith("1402-01-01-3.jpg");
    }
}