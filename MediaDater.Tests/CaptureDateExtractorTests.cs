using AwesomeAssertions;
using Xunit;

namespace MediaDater.Tests;

public class CaptureDateExtractorTests
{
    [Theory]
    [InlineData("IMG_20231005_123045.jpg", 2023, 10, 5, 12, 30, 45, 0)]
    [InlineData("VID_20220101_000000.mp4", 2022, 1, 1, 0, 0, 0, 0)]
    [InlineData("Screenshot_2023-11-20-15-45-10-123.png", 2023, 11, 20, 15, 45, 10, 123)]
    public void Extract_FilenamePatterns_ReturnsParsedDate(string fileName, int year, int month, int day, int h, int m, int s, int ms)
    {
        DateTime? result = CaptureDateExtractor.Extract(fileName);

        result.Should().NotBeNull();
        result!.Value.Should().Be(new(year, month, day, h, m, s, ms));
    }

    [Theory]
    [InlineData("img_20231005_123045.jpg")]
    [InlineData("Img_20231005_123045.jpg")]
    [InlineData("IMG_20231005_123045.jpg")]
    [InlineData("screenshot_2023-11-20-15-45-10-123.png")]
    [InlineData("ScreenRecorder_2023-11-20-15-45-10-123.png")]
    public void Extract_CaseInsensitivePatterns_ReturnsParsedDate(string fileName)
    {
        DateTime? result = CaptureDateExtractor.Extract(fileName);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Extract_InvalidPattern_ReturnsNull()
    {
        string fileName = "RandomFile.txt";

        DateTime? result = CaptureDateExtractor.Extract(fileName);

        result.Should().BeNull();
    }

    [Fact]
    public void Extract_NonExistentFileWithNoMetadata_ReturnsNull()
    {
        string fileName = "NonExistentFile.jpg";

        DateTime? result = CaptureDateExtractor.Extract(fileName);

        result.Should().BeNull();
    }
}