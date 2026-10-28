using System.Globalization;

namespace MediaDater;

internal class FileNameGenerator(string directoryPath)
{
    private static readonly PersianCalendar PersianCalendar = new();

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
        string targetPath = Path.Combine(directoryPath, baseName + extension);

        if (!File.Exists(targetPath))
        {
            return targetPath;
        }

        int counter = 1;

        while (true)
        {
            string candidate = Path.Combine(directoryPath, $"{baseName}-{counter}{extension}");

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