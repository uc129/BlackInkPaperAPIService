using System.Globalization;

namespace BlackInkPaperAdmin.Services.Shared;

public static class CurrencyHelper
{
    private static readonly CultureInfo InrCulture = new("en-IN");

    public static string FmtINR(decimal amount) => amount.ToString("C0", InrCulture);
}
