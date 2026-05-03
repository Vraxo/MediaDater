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
    private const string Mauve = "\x1b[38;2;203;166;247m";   // #cba6f7

    // Semantic mappings
    private const string ColorSuccess = Green;
    private const string ColorError = Red;
    private const string ColorWarning = Yellow;
    private const string ColorInfo = Blue;
    private const string ColorFilename = Teal;
    private const string ColorSummary = Mauve;   // used for summary labels

    // Fixed width for the entire tag including brackets (e.g., "[ERROR]" = 7)
    private const int TagWidth = 7;
    private const int AlignWidth = 7; // for summary lines

    // Helper: write left-aligned tag, then pad spaces after the bracket to reach TagWidth
    private static void WriteTag(string color, string text)
    {
        Console.Write("[");
        Console.Write(color);
        Console.Write(text);
        Console.Write(Reset);
        Console.Write("]");
        int visibleLength = text.Length + 2; // +2 for brackets
        int padding = TagWidth - visibleLength;
        if (padding > 0)
        {
            Console.Write(new string(' ', padding));
        }
        // No extra space – message starts immediately after padding
    }

    public static void Ok(string originalName, string newName)
    {
        WriteTag(ColorSuccess, "OK");
        Console.Write(ColorFilename);
        Console.Write(originalName);
        Console.Write(Reset);
        Console.Write(" -> ");
        Console.Write(ColorFilename);
        Console.Write(newName);
        Console.Write(Reset);
        Console.WriteLine();
    }

    public static void Error(string message)
    {
        WriteTag(ColorError, "ERROR");
        Console.Write(ColorError);
        Console.WriteLine(message);
        Console.Write(Reset);
    }

    public static void Warning(string message)
    {
        WriteTag(ColorWarning, "WARN");
        Console.Write(ColorWarning);
        Console.WriteLine(message);
        Console.Write(Reset);
    }

    public static void Info(string message)
    {
        Console.Write(ColorInfo);
        Console.WriteLine(message);
        Console.Write(Reset);
    }

    public static void AlignedInfo(string label, string value)
    {
        string paddedLabel = label.PadRight(AlignWidth);
        Console.Write(ColorSummary);
        Console.Write(paddedLabel);
        Console.Write(": ");
        Console.Write(White);
        Console.Write(value);
        Console.Write(Reset);
        Console.WriteLine();
    }

    public static void Success(string label, string value)
    {
        string paddedLabel = label.PadRight(AlignWidth);
        Console.Write(ColorSummary);
        Console.Write(paddedLabel);
        Console.Write(": ");
        Console.Write(White);
        Console.Write(value);
        Console.Write(Reset);
        Console.WriteLine();
    }

    public static void Skip(string filename)
    {
        WriteTag(ColorInfo, "SKIP");
        Console.Write(White);
        Console.Write(filename);
        Console.Write(Reset);
        Console.WriteLine();
    }
}