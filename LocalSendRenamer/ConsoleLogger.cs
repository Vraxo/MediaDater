using Spectre.Console;

namespace LocalSendRenamer;

public static class ConsoleLogger
{
    private const int TagWidth = 7;
    private const int AlignWidth = 7;

    private static string GetTagMarkup(string color, string text)
    {
        string tag = $"[[[{color}]{text}[/]]]";
        int padding = TagWidth - (text.Length + 2);
        return padding > 0 ? tag + new string(' ', padding) : tag;
    }

    public static void Ok(string originalName, string newName)
    {
        string escapedOrig = Markup.Escape(originalName);
        string escapedNew = Markup.Escape(newName);
        AnsiConsole.MarkupLine($"{GetTagMarkup("green", "OK")}[teal]{escapedOrig}[/] -> [teal]{escapedNew}[/]");
    }

    public static void Error(string message)
    {
        AnsiConsole.MarkupLine($"{GetTagMarkup("red", "ERROR")}[red]{Markup.Escape(message)}[/]");
    }

    public static void Warning(string message)
    {
        AnsiConsole.MarkupLine($"{GetTagMarkup("yellow", "WARN")}[yellow]{Markup.Escape(message)}[/]");
    }

    public static void Info(string message)
    {
        AnsiConsole.MarkupLine($"[blue]{Markup.Escape(message)}[/]");
    }

    public static void LogAligned(string label, string value)
    {
        string paddedLabel = label.PadRight(AlignWidth);
        AnsiConsole.MarkupLine($"[purple]{Markup.Escape(paddedLabel)}[/]: [white]{Markup.Escape(value)}[/]");
    }

    public static void Skip(string filename)
    {
        AnsiConsole.MarkupLine($"{GetTagMarkup("blue", "SKIP")}[white]{Markup.Escape(filename)}[/]");
    }
}