using System.Text.Json;

namespace LocalSendRenamer;

public record RenameManifestEntry(string OriginalPath, string NewPath);

public static class ManifestManager
{
    private const string ManifestFileName = ".rename_manifest.json";

    public static void Save(string directory, IEnumerable<FileRenameInfo> renames)
    {
        List<RenameManifestEntry> entries = renames.Select(r => new RenameManifestEntry(r.OriginalPath, r.NewPath)).ToList();
        string manifestPath = Path.Combine(directory, ManifestFileName);
        string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
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