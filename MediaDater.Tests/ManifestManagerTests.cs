using AwesomeAssertions;
using Xunit;

namespace MediaDater.Tests;

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
        List<FileRenameInfo> originalEntries =
        [
            new(@"C:\test\old1.jpg", @"C:\test\new1.jpg"),
            new(@"C:\test\old2.mp4", @"C:\test\new2.mp4")
        ];
        string manifestPath = Path.Combine(_tempDir, ".rename_manifest.json");
        string json = System.Text.Json.JsonSerializer.Serialize(originalEntries);
        File.WriteAllText(manifestPath, json);

        List<FileRenameInfo> loaded = ManifestManager.Load(_tempDir);

        loaded.Should().HaveCount(2);
        loaded[0].OriginalPath.Should().Be(@"C:\test\old1.jpg");
        loaded[0].NewPath.Should().Be(@"C:\test\new1.jpg");
        loaded[1].OriginalPath.Should().Be(@"C:\test\old2.mp4");
        loaded[1].NewPath.Should().Be(@"C:\test\new2.mp4");
    }

    [Fact]
    public void Load_WhenNoManifest_ReturnsEmptyList()
    {
        List<FileRenameInfo> loaded = ManifestManager.Load(_tempDir);
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