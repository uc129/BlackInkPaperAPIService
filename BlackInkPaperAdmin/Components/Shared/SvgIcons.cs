namespace BlackInkPaperAdmin.Components.Shared;

public static class SvgIcons
{
    public static string Get(string name) =>
        Icons.TryGetValue(name, out var v) ? v : "";

    private static readonly Dictionary<string, string> Icons = new()
    {
        ["home"]        = """<path d="M3 11l9-8 9 8"/><path d="M5 10v10h14V10"/>""",
        ["package"]     = """<path d="M21 8l-9-5-9 5 9 5 9-5z"/><path d="M3 8v9l9 5 9-5V8"/><path d="M12 13v9"/>""",
        ["layers"]      = """<path d="M12 3l9 5-9 5-9-5 9-5z"/><path d="M3 13l9 5 9-5"/><path d="M3 18l9 5 9-5"/>""",
        ["tag"]         = """<path d="M20 12l-8 8-9-9V3h8z"/><circle cx="7" cy="7" r="1.2"/>""",
        ["user"]        = """<circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 4-7 8-7s8 3 8 7"/>""",
        ["users"]       = """<circle cx="9" cy="8" r="3.5"/><path d="M2 21c0-4 3-6 7-6s7 2 7 6"/><path d="M16 4a3.5 3.5 0 010 7"/><path d="M22 21c0-3-2-5-5-5"/>""",
        ["settings"]    = """<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 00.3 1.8l.1.1a2 2 0 11-2.8 2.8l-.1-.1a1.7 1.7 0 00-1.8-.3 1.7 1.7 0 00-1 1.5V21a2 2 0 11-4 0v-.1a1.7 1.7 0 00-1-1.5 1.7 1.7 0 00-1.8.3l-.1.1a2 2 0 11-2.8-2.8l.1-.1a1.7 1.7 0 00.3-1.8 1.7 1.7 0 00-1.5-1H3a2 2 0 110-4h.1a1.7 1.7 0 001.5-1 1.7 1.7 0 00-.3-1.8l-.1-.1a2 2 0 112.8-2.8l.1.1a1.7 1.7 0 001.8.3H9a1.7 1.7 0 001-1.5V3a2 2 0 114 0v.1a1.7 1.7 0 001 1.5 1.7 1.7 0 001.8-.3l.1-.1a2 2 0 112.8 2.8l-.1.1a1.7 1.7 0 00-.3 1.8V9a1.7 1.7 0 001.5 1H21a2 2 0 110 4h-.1a1.7 1.7 0 00-1.5 1z"/>""",
        ["search"]      = """<circle cx="11" cy="11" r="7"/><path d="M21 21l-4.3-4.3"/>""",
        ["plus"]        = """<path d="M12 5v14M5 12h14"/>""",
        ["minus"]       = """<path d="M5 12h14"/>""",
        ["filter"]      = """<path d="M3 5h18l-7 9v6l-4-2v-4z"/>""",
        ["arrowDown"]   = """<path d="M12 5v14M19 12l-7 7-7-7"/>""",
        ["arrowUp"]     = """<path d="M12 19V5M5 12l7-7 7 7"/>""",
        ["chevronDown"] = """<path d="M6 9l6 6 6-6"/>""",
        ["chevronRight"]= """<path d="M9 6l6 6-6 6"/>""",
        ["chevronUp"]   = """<path d="M6 15l6-6 6 6"/>""",
        ["more"]        = """<circle cx="5" cy="12" r="1.4"/><circle cx="12" cy="12" r="1.4"/><circle cx="19" cy="12" r="1.4"/>""",
        ["edit"]        = """<path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 113 3L7 19l-4 1 1-4z"/>""",
        ["trash"]       = """<path d="M3 6h18"/><path d="M8 6V4a2 2 0 012-2h4a2 2 0 012 2v2"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/>""",
        ["eye"]         = """<path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>""",
        ["check"]       = """<path d="M5 12l5 5L20 7"/>""",
        ["x"]           = """<path d="M6 6l12 12M6 18L18 6"/>""",
        ["bell"]        = """<path d="M6 8a6 6 0 1112 0c0 7 3 9 3 9H3s3-2 3-9z"/><path d="M10 21a2 2 0 004 0"/>""",
        ["star"]        = """<path d="M12 3l3 6 7 1-5 5 1 7-6-3-6 3 1-7-5-5 7-1z"/>""",
        ["image"]       = """<rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="9" cy="9" r="2"/><path d="M21 15l-5-5L5 21"/>""",
        ["upload"]      = """<path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><path d="M17 8l-5-5-5 5"/><path d="M12 3v12"/>""",
        ["download"]    = """<path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><path d="M7 10l5 5 5-5"/><path d="M12 15V3"/>""",
        ["alert"]       = """<circle cx="12" cy="12" r="9"/><path d="M12 8v4M12 16h.01"/>""",
        ["info"]        = """<circle cx="12" cy="12" r="9"/><path d="M12 8h.01M11 12h1v4h1"/>""",
        ["refresh"]     = """<path d="M3 12a9 9 0 0115-6.7L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 01-15 6.7L3 16"/><path d="M3 21v-5h5"/>""",
        ["grid3"]       = """<rect x="3" y="3" width="6" height="6" rx="1"/><rect x="15" y="3" width="6" height="6" rx="1"/><rect x="3" y="15" width="6" height="6" rx="1"/><rect x="15" y="15" width="6" height="6" rx="1"/>""",
        ["rows"]        = """<rect x="3" y="4" width="18" height="4" rx="1"/><rect x="3" y="11" width="18" height="4" rx="1"/><rect x="3" y="18" width="18" height="4" rx="1"/>""",
        ["ruler"]       = """<path d="M3 17l8-8 4 4-8 8z"/><path d="M14 6l4-4 4 4-4 4"/><path d="M11 9l1.5 1.5M8 12l1.5 1.5M5 15l1.5 1.5"/>""",
        ["logout"]      = """<path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/><path d="M16 17l5-5-5-5"/><path d="M21 12H9"/>""",
        ["lock"]        = """<rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 018 0v4"/>""",
        ["mail"]        = """<rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 7l10 7 10-7"/>""",
        ["rupee"]       = """<path d="M6 3h12M6 8h12M14.5 3c0 5-3 8-7 8h2l7 10"/>""",
        ["receipt"]     = """<path d="M4 2v20l2-1 2 1 2-1 2 1 2-1 2 1 2-1 2 1V2l-2 1-2-1-2 1-2-1-2 1-2-1z"/><path d="M8 7h8M8 11h8M8 15h4"/>""",
        ["truck"]       = """<path d="M1 3h11v11H1z"/><path d="M12 8h4l3 5v3h-7V8z"/><circle cx="5.5" cy="17.5" r="2.5"/><circle cx="18.5" cy="17.5" r="2.5"/>""",
    };
}
