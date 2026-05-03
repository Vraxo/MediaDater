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

    // Semantic mappings
    private const string ColorSuccess = Green;
    private const string ColorError = Red;
    private const string ColorWarning = Yellow;
    private const string ColorInfo = Blue;
    private const string ColorFilename = Teal;

    public static void Ok(string originalName, string newName)
    {
        Console.Write("[");
        Console.Write(ColorSuccess);
        Console.Write("OK");
        Console.Write(Reset);
        Console.Write("] ");
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
        Console.Write(ColorError);
        Console.WriteLine(message);
        Console.Write(Reset);
    }

    public static void Warning(string message)
    {
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

    public static void Success(string label, string value)
    {
        const int labelWidth = 14;
        string paddedLabel = label.PadRight(labelWidth);
        Console.Write(ColorSuccess);
        Console.Write($"{paddedLabel}: {value}");
        Console.Write(Reset);
        Console.WriteLine();
    }

    public static void Skip(string filename)
    {
        Console.Write(ColorInfo);
        Console.Write("Skipping already correctly named: ");
        Console.Write(White);
        Console.Write(filename);
        Console.Write(Reset);
        Console.WriteLine();
    }
}