using FluentAssertions;
using Xunit;

namespace MediaDater.Tests;

public class DateValidatorTests
{
    [Fact]
    public void AreAllPlausible_AllDatesWithinRange_ReturnsTrue()
    {
        List<FileEntry> entries =
        [
            new("file1.jpg", new(2025, 1, 1)),
            new("file2.jpg", new(2030, 12, 31)),
            new("file3.jpg", new(2000, 6, 15))
        ];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeTrue();
    }

    [Fact]
    public void AreAllPlausible_WithDateBeforeMin_ReturnsFalse()
    {
        List<FileEntry> entries =
        [
            new("file1.jpg", new(2025, 1, 1)),
            new("file2.jpg", new(1999, 12, 31))
        ];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeFalse();
    }

    [Fact]
    public void AreAllPlausible_WithDateAfterMax_ReturnsFalse()
    {
        List<FileEntry> entries =
        [
            new("file1.jpg", new(2031, 1, 1))
        ];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeFalse();
    }

    [Fact]
    public void AreAllPlausible_EmptyEntries_ReturnsTrue()
    {
        List<FileEntry> entries = [];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeTrue();
    }

    [Fact]
    public void AreAllPlausible_AtExactBoundaries_ReturnsTrue()
    {
        List<FileEntry> entries =
        [
            new("min.jpg", new(2000, 1, 1)),
            new("max.jpg", new(2030, 12, 31, 23, 59, 59))
        ];

        bool result = DateValidator.AreAllPlausible(entries, 2000, 2030);

        result.Should().BeTrue();
    }
}