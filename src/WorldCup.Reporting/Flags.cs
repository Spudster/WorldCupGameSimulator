namespace WorldCup.Reporting;

/// <summary>Maps a 3-letter FIFA team code to a flag emoji (a regional-indicator pair). Returns "" for
/// unknown codes and for the England/Scotland/Wales sub-nations (whose tag-sequence flags don't render
/// on Windows), so those show as the plain team name rather than a bare black flag.</summary>
public static class Flags
{
    // FIFA code → ISO 3166-1 alpha-2 (or a "GB-XXX" marker for the home nations).
    private static readonly Dictionary<string, string> Iso = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ENG"] = "GB-ENG", ["SCO"] = "GB-SCT", ["WAL"] = "GB-WLS", ["NIR"] = "GB",
        ["BRA"] = "BR", ["ARG"] = "AR", ["ESP"] = "ES", ["FRA"] = "FR", ["GER"] = "DE", ["POR"] = "PT",
        ["NED"] = "NL", ["BEL"] = "BE", ["ITA"] = "IT", ["CRO"] = "HR", ["URU"] = "UY", ["COL"] = "CO",
        ["MEX"] = "MX", ["USA"] = "US", ["CAN"] = "CA", ["JPN"] = "JP", ["KOR"] = "KR", ["AUS"] = "AU",
        ["MAR"] = "MA", ["SEN"] = "SN", ["SUI"] = "CH", ["DEN"] = "DK", ["SRB"] = "RS", ["POL"] = "PL",
        ["UKR"] = "UA", ["AUT"] = "AT", ["TUR"] = "TR", ["NOR"] = "NO", ["SWE"] = "SE", ["ECU"] = "EC",
        ["PER"] = "PE", ["CHI"] = "CL", ["PAR"] = "PY", ["BOL"] = "BO", ["VEN"] = "VE", ["GHA"] = "GH",
        ["NGA"] = "NG", ["CIV"] = "CI", ["CMR"] = "CM", ["TUN"] = "TN", ["ALG"] = "DZ", ["EGY"] = "EG",
        ["RSA"] = "ZA", ["MLI"] = "ML", ["BFA"] = "BF", ["GUI"] = "GN", ["GAB"] = "GA", ["ANG"] = "AO",
        ["ZAM"] = "ZM", ["KEN"] = "KE", ["UGA"] = "UG", ["COD"] = "CD", ["CPV"] = "CV", ["IRN"] = "IR",
        ["KSA"] = "SA", ["QAT"] = "QA", ["UAE"] = "AE", ["IRQ"] = "IQ", ["JOR"] = "JO", ["UZB"] = "UZ",
        ["BHR"] = "BH", ["OMA"] = "OM", ["KUW"] = "KW", ["LBN"] = "LB", ["SYR"] = "SY", ["PLE"] = "PS",
        ["CHN"] = "CN", ["IND"] = "IN", ["THA"] = "TH", ["VIE"] = "VN", ["IDN"] = "ID", ["INA"] = "ID",
        ["CRC"] = "CR", ["PAN"] = "PA", ["HON"] = "HN", ["JAM"] = "JM", ["TRI"] = "TT", ["HAI"] = "HT",
        ["GUA"] = "GT", ["SLV"] = "SV", ["CUB"] = "CU", ["SUR"] = "SR", ["CUW"] = "CW", ["NZL"] = "NZ",
        ["FIJ"] = "FJ", ["NCL"] = "NC", ["TAH"] = "PF", ["SOL"] = "SB", ["VAN"] = "VU", ["PNG"] = "PG",
        ["CZE"] = "CZ", ["SVK"] = "SK", ["SVN"] = "SI", ["ROU"] = "RO", ["HUN"] = "HU", ["GRE"] = "GR",
        ["IRL"] = "IE", ["ISL"] = "IS", ["FIN"] = "FI", ["RUS"] = "RU", ["BIH"] = "BA", ["MKD"] = "MK",
    };

    public static string Of(string code)
    {
        if (string.IsNullOrEmpty(code) || !Iso.TryGetValue(code, out var iso))
        {
            return "";
        }

        return iso switch
        {
            // The England/Scotland/Wales sub-nation flags use Unicode tag sequences that Windows can't
            // render (they fall back to a bare black flag 🏴), so show no flag and let the name stand alone.
            "GB-ENG" or "GB-SCT" or "GB-WLS" => "",
            _ when iso.Length == 2 =>
                char.ConvertFromUtf32(0x1F1E6 + (char.ToUpperInvariant(iso[0]) - 'A')) +
                char.ConvertFromUtf32(0x1F1E6 + (char.ToUpperInvariant(iso[1]) - 'A')),
            _ => "",
        };
    }

    /// <summary>"🇪🇸 Spain" — flag + name, or just the name when no flag is known.</summary>
    public static string Named(string code, string name)
    {
        string f = Of(code);
        return f.Length == 0 ? name : f + " " + name;
    }
}
