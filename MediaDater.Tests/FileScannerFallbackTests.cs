using FluentAssertions;
using Xunit;

namespace MediaDater.Tests;

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