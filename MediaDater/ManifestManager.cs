using System.Text.Json;

namespace MediaDater;

public static class ManifestManager
{
    private const string ManifestFileName = ".rename_manifest.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Save(string directory, IEnumerable<FileRenameInfo> renames)
    {
        string manifestPath = Path.Combine(directory, ManifestFileName);
        string json = JsonSerializer.Serialize(renames, JsonOptions);

        File.WriteAllText(manifestPath, json);
    }

    public static List<FileRenameInfo> Load(string directory)
    {
        string manifestPath = Path.Combine(directory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return [];
        }

        string json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<List<FileRenameInfo>>(json) ?? [];
    }

    public static void Delete(string directory)
    {
        string manifestPath = Path.Combine(directory, ManifestFileName);

        if (File.Exists(manifestPath))
        {
            File.Delete(manifestPath);
        }
    }
}