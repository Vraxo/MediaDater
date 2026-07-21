namespace LocalSendRenamer;

public static class ConsoleLogger
{
    // Catppuccin Mocha ANSI colors
    private const string Reset = "\x1b[0m";
    private const string White = "\x1b[38;2;255;255;255m";
    private const string Teal = "\x1b[38;2;148;226;213m";
    private const string Blue = "\x1b[38;2;137;180;250m";
    private const string Green = "\x1b[38;2;166;218;149m";
    private const string Yellow = "\x1b[38;2;249;226;175m";
    private const string Red = "\x1b[38;2;243;139;168m";
    private const string Mauve = "\x1b[38;2;203;166;247m";

    // Semantic mappings
    private const string ColorSuccess = Green;
    private const string ColorError = Red;
    private const string ColorWarning = Yellow;
    private const string ColorInfo = Blue;
    private const string ColorFilename = Teal;
    private const string ColorSummary = Mauve;

    private const int TagWidth = 7;
    private const int AlignWidth = 7;

    private static string GetTag(string color, string text)
    {
        string tag = $"[{color}{text}{Reset}]";
        int padding = TagWidth - (text.Length + 2);
        return padding > 0 ? tag + new string(' ', padding) : tag;
    }

    public static void Ok(string originalName, string newName)
    {
        Console.WriteLine($"{GetTag(ColorSuccess, "OK")}{ColorFilename}{originalName}{Reset} -> {ColorFilename}{newName}{Reset}");
    }

    public static void Error(string message)
    {
        Console.WriteLine($"{GetTag(ColorError, "ERROR")}{ColorError}{message}{Reset}");
    }

    public static void Warning(string message)
    {
        Console.WriteLine($"{GetTag(ColorWarning, "WARN")}{ColorWarning}{message}{Reset}");
    }

    public static void Info(string message)
    {
        Console.WriteLine($"{ColorInfo}{message}{Reset}");
    }

    public static void LogAligned(string label, string value)
    {
        string paddedLabel = label.PadRight(AlignWidth);
        Console.WriteLine($"{ColorSummary}{paddedLabel}: {White}{value}{Reset}");
    }

    public static void Skip(string filename)
    {
        Console.WriteLine($"{GetTag(ColorInfo, "SKIP")}{White}{filename}{Reset}");
    }
}