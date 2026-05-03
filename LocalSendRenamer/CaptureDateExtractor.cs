using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalSendRenamer;

public partial class CaptureDateExtractor
{
    [GeneratedRegex(@"^(IMG|VID)_(\d{8})_(\d{6})", RegexOptions.IgnoreCase)]
    private static partial Regex TimestampRegex();

    [GeneratedRegex(@"^Screen(?:shot|recorder)[-_](\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{3})", RegexOptions.IgnoreCase)]
    private static partial Regex ScreenshotRegex();

    public static DateTime? Extract(string filePath)
    {
        return GetFilenameDate(filePath)
            ?? GetMetadataDate(filePath);
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
        if (!IsDateValid(input, format, out DateTime result))
        {
            return null;
        }

        return result;
    }

    private static bool IsDateValid(string input, string format, out DateTime result)
    {
        return DateTime.TryParseExact(
            input,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
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
        ExifSubIfdDirectory? subIfd = GetSubIfd(directories);

        if (subIfd?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime date) == true)
        {
            return date;
        }

        ExifIfd0Directory? ifd0 = GetIfd0(directories);

        if (ifd0?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out date) == true)
        {
            return date;
        }

        return null;
    }

    private static ExifSubIfdDirectory? GetSubIfd(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        return directories
            .OfType<ExifSubIfdDirectory>()
            .FirstOrDefault();
    }

    private static ExifIfd0Directory? GetIfd0(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        return directories
            .OfType<ExifIfd0Directory>()
            .FirstOrDefault();
    }

    private static DateTime? GetQuickTimeDate(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        QuickTimeMovieHeaderDirectory? movieHeader = GetMovieHeader(directories);

        if ((movieHeader?.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out DateTime date)) != true)
        {
            return null;
        }

        return DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime();
    }

    private static QuickTimeMovieHeaderDirectory? GetMovieHeader(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        return directories
            .OfType<QuickTimeMovieHeaderDirectory>()
            .FirstOrDefault();
    }
}