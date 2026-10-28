using AwesomeAssertions;
using Xunit;

namespace MediaDater.Tests;

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