namespace LocalSendRenamer;

public static class DateValidator
{
    public static bool AreAllPlausible(IEnumerable<FileEntry> entries, int minYear, int maxYear)
    {
        bool allOk = true;

        foreach (FileEntry entry in entries)
        {
            DateTime date = entry.CaptureTime;

            if (date.Year < minYear || date.Year > maxYear)
            {
                ConsoleLogger.Error($"Implausible date {date:yyyy-MM-dd} for file: {entry.Path}");
                allOk = false;
            }
        }

        return allOk;
    }
}