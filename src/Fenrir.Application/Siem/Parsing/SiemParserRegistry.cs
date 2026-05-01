namespace Fenrir.Application.Siem.Parsing;

public sealed class SiemParserRegistry(IEnumerable<ISiemParser> parsers) : ISiemParserRegistry
{
    private readonly IReadOnlyDictionary<string, ISiemParser> _parsers = parsers
        .GroupBy(parser => parser.ParserName, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    public ISiemParser GetParser(string parserName)
    {
        if (!string.IsNullOrWhiteSpace(parserName) && _parsers.TryGetValue(parserName.Trim(), out var parser))
        {
            return parser;
        }

        return _parsers.TryGetValue(GenericJsonSiemParser.Name, out var fallback)
            ? fallback
            : throw new InvalidOperationException("No SIEM parser is registered.");
    }

    public IReadOnlyCollection<ISiemParser> ListParsers() => _parsers.Values.ToArray();
}
