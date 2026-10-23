using System.Globalization;

namespace FarmMitra.Services;

public static class Csv
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Row(params object?[] cells) => string.Join(",", cells.Select(Cell));

    private static string Cell(object? value)
    {
        switch (value)
        {
            case null: return "";
            case decimal d: return d.ToString("0.##", Inv);
            case double f: return f.ToString("0.##", Inv);
            case int or long: return Convert.ToString(value, Inv) ?? "";
            case bool b: return b ? "Yes" : "No";
            case DateTime dt: return dt.ToString("yyyy-MM-dd HH:mm", Inv);
        }

        var s = (value.ToString() ?? "").Replace("\r", " ").Replace("\n", " ");

        // Text typed by users must never run as a spreadsheet formula (CSV injection).
        if (s.Length > 0 && "=+-@\t".Contains(s[0])) s = "'" + s;

        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
