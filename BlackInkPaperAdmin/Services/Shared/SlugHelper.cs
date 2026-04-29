using System.Text.RegularExpressions;

namespace BlackInkPaperAdmin.Services.Shared;

public static class SlugHelper
{
    public static string Slugify(string s) =>
        Regex.Replace(s.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');
}
