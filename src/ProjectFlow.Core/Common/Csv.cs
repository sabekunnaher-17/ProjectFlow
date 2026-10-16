namespace ProjectFlow.Core.Common;

public static class Csv
{
    public static string Cell(string? text)
    {
        text ??= "";
        // Prevent spreadsheet formulas, including those prefixed by whitespace.
        if (text.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' || text.StartsWith('\t') || text.StartsWith('\r')) text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
