using FluentAssertions;
using Xunit;

namespace LocalSendRenamer.Tests;

public class RenamePlannerTests
{
    [Fact]
    public void Plan_EmptyEntries_ReturnsEmpty()
    {
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Plan_SingleFile_ReturnsSingleBatch()
    {
        FileEntry[] entries = [new("C:\\Photos\\test.jpg", new(2023, 3, 21))];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        result.Should().HaveCount(1);
        result[0].Directory.Should().Be("C:\\Photos");
        result[0].Renames.Should().HaveCount(1);
    }

    [Fact]
    public void Plan_MultipleDirectories_GroupsByDirectory()
    {
        FileEntry[] entries =
        [
            new("C:\\Photos\\a.jpg", new(2023, 3, 21)),
            new("C:\\Videos\\b.jpg", new(2023, 3, 21))
        ];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Directory == "C:\\Photos");
        result.Should().Contain(b => b.Directory == "C:\\Videos");
    }

    [Fact]
    public void Plan_SameDateDifferentFiles_GroupsByDate()
    {
        FileEntry[] entries =
        [
            new("C:\\Photos\\morning.jpg", new(2023, 3, 21, 8, 0, 0)),
            new("C:\\Photos\\evening.jpg", new(2023, 3, 21, 18, 0, 0))
        ];

        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        result.Should().HaveCount(1);
        result[0].Renames.Should().HaveCount(2);
    }

    [Fact]
    public void Plan_DifferentDates_SeparateBatches()
    {
        FileEntry[] entries =
        [
            new("C:\\Photos\\day1.jpg", new(2023, 3, 21)),
            new("C:\\Photos\\day2.jpg", new(2023, 3, 22))
        ];

        RenamePlanner planner = new("C:\\", [".jpg"]);

        List<RenameBatch> result = [.. planner.Plan(entries)];

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Plan_InvalidExtension_Excluded()
    {
        FileEntry[] entries = [new("C:\\Photos\\test.txt", new(2023, 3, 21))];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan(entries);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Plan_DefaultDate_Excluded()
    {
        FileEntry[] entries = [new("C:\\Photos\\test.jpg", default)];
        RenamePlanner planner = new("C:\\", [".jpg"]);

        IEnumerable<RenameBatch> result = planner.Plan(entries);

        result.Should().BeEmpty();
    }
}