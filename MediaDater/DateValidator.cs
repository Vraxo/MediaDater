namespace MediaDater;

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