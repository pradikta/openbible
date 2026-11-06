namespace OpenBible.Infrastructure.Sword;

internal static class SwordModuleConfParser
{
    public static SwordModuleConf Parse(string text, string defaultName)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var name = defaultName;
        var currentKey = string.Empty;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                name = line[1..^1];
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator > 0)
            {
                currentKey = line[..separator].Trim();
                values[currentKey] = line[(separator + 1)..].Trim();
            }
            else if (currentKey.Length > 0)
            {
                values[currentKey] += "\n" + line;
            }
        }

        string Get(string key, string fallback = "") =>
            values.TryGetValue(key, out var value) && value.Length > 0 ? value : fallback;

        var dataPath = Get("datapath").Replace('\\', '/').Trim('/');
        if (dataPath.Length == 0)
        {
            throw new InvalidDataException("SWORD module config has no DataPath.");
        }

        var description = Get("description");
        if (description.Length == 0)
        {
            description = Get("about", name);
        }

        var license = Get("distributionlicense");
        if (license.Length == 0)
        {
            license = Get("copyright");
        }

        return new SwordModuleConf(
            name,
            dataPath,
            Get("moddrv", "zText"),
            Get("encoding") is { Length: > 0 } encoding ? encoding : null,
            Get("blocktype", "BOOK"),
            Get("compresstype", "ZIP"),
            Get("versification", "KJV"),
            Get("sourcetype", "OSIS"),
            description,
            Get("lang") is { Length: > 0 } language ? language : null,
            license.Length > 0 ? license : null);
    }
}
