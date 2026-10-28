using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaDater;

public partial class CaptureDateExtractor
{
    [GeneratedRegex(@"^(IMG|VID)_(\d{8})_(\d{6})", RegexOptions.IgnoreCase)]
    private static partial Regex TimestampRegex();

    [GeneratedRegex(@"^Screen(?:shot|recorder)[-_](\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{3})", RegexOptions.IgnoreCase)]
    private static partial Regex ScreenshotRegex();

    public static DateTime? Extract(string filePath)
    {
        DateTime? date = GetMetadataDate(filePath);
        if (date.HasValue && IsPlausible(date.Value))
        {
            return date;
        }

        date = GetFilenameDate(filePath);
        if (date.HasValue && IsPlausible(date.Value))
        {
            return date;
        }

        return null;
    }

    private static bool IsPlausible(DateTime date)
    {
        return date.Year is >= DateValidator.MinPlausibleYear and <= DateValidator.MaxPlausibleYear;
    }

    private static DateTime? GetFilenameDate(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        return ParseStandardTimestamp(fileName)
            ?? ParseScreenshotTimestamp(fileName);
    }

    private static DateTime? ParseStandardTimestamp(string fileName)
    {
        Match match = TimestampRegex().Match(fileName);

        if (!match.Success)
        {
            return null;
        }

        string timestamp = match.Groups[2].Value + match.Groups[3].Value;
        return TryParseDate(timestamp, "yyyyMMddHHmmss");
    }

    private static DateTime? ParseScreenshotTimestamp(string fileName)
    {
        Match match = ScreenshotRegex().Match(fileName);

        if (!match.Success)
        {
            return null;
        }

        return TryParseDate(match.Groups[1].Value, "yyyy-MM-dd-HH-mm-ss-fff");
    }

    private static DateTime? TryParseDate(string input, string format)
    {
        bool success = DateTime.TryParseExact(
            input,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime result);

        return success ? result : null;
    }

    private static DateTime? GetMetadataDate(string filePath)
    {
        try
        {
            IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(filePath);

            return GetExifDate(directories)
                ?? GetQuickTimeDate(directories);
        }
        catch (Exception ex) when (ex is ImageProcessingException or FileNotFoundException)
        {
            return null;
        }
    }

    private static DateTime? GetExifDate(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        ExifSubIfdDirectory? subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        if (subIfd?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime date) == true)
        {
            return date;
        }

        ExifIfd0Directory? ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        if (ifd0?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out date) == true)
        {
            return date;
        }

        return null;
    }

    private static DateTime? GetQuickTimeDate(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        QuickTimeMovieHeaderDirectory? movieHeader = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
        if (movieHeader?.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out DateTime date) ?? false)
        {
            return DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime();
        }

        return null;
    }
}