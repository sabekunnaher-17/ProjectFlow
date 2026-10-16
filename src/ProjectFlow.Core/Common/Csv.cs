namespace ProjectFlow.Core.Common;

public static class Csv
{
    public static string Cell(string? text)
    {
        text ??= "";
        // Prevent formulas hidden behind whitespace, controls or a byte-order mark.
        var index = 0;
        while (index < text.Length && (char.IsWhiteSpace(text[index]) || char.IsControl(text[index]) || text[index] == '\uFEFF'))
            index++;
        if ((index < text.Length && text[index] is '=' or '+' or '-' or '@') || (text.Length > 0 && char.IsControl(text[0])))
            text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
