// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Captures declaration-header candidates from a complete immutable source snapshot before commands parse.
/// This is not a successful-parse inventory: duplicates remain candidates and ordering never selects one.
/// </summary>
internal sealed partial class CommandStreamCandidates
{
    readonly Dictionary<string, List<string[]>> _sources;
    readonly HashSet<string> _qualifiedTypes;

    CommandStreamCandidates(Dictionary<string, List<string[]>> sources, HashSet<string> qualifiedTypes)
    {
        _sources = sources;
        _qualifiedTypes = qualifiedTypes;
    }

    internal static CommandStreamCandidates Capture(IEnumerable<IReadOnlyList<SourceLine>> documents)
    {
        var sources = new Dictionary<string, List<string[]>>(StringComparer.Ordinal);
        var types = new HashSet<string>(StringComparer.Ordinal);
        var imports = new HashSet<string>(StringComparer.Ordinal);
        foreach (var lines in documents)
        {
            var reader = new LineReader(lines);
            while (reader.PeekSignificant() is { } header)
            {
                reader.TakeSignificant();
                var source = SourceHeaderRegex().Match(header.Content);
                var streams = new List<string>();
                if (TypeHeaderRegex().Match(header.Content) is { Success: true } type) types.Add(type.Groups[1].Success ? type.Groups[1].Value : type.Groups[2].Value);
                if (ImportHeaderRegex().Match(header.Content) is { Success: true } import) imports.Add(import.Groups[1].Value);

                // Only document-level declarations participate. Fences are opaque even when their raw
                // contents are unindented. Command properties retain their deliberately different leaf rule.
                while (reader.PeekSignificant() is { } child && child.Indent > header.Indent)
                {
                    reader.TakeSignificant();
                    if (source.Success && StreamHeaderRegex().Match(child.Content) is { Success: true } stream) streams.Add(stream.Groups[1].Value);
                    SkipChildren(reader, child);
                }
                if (source.Success)
                {
                    if (!sources.TryGetValue(source.Groups[1].Value, out var parents)) sources[source.Groups[1].Value] = parents = [];
                    parents.Add([.. streams]);
                }
            }
        }

        // A contract import by itself does not reveal its kind. A real concept/composite declaration
        // establishes a viable property interpretation without broadening event/operation namespaces.
        var qualified = imports.Where(import => types.Contains(import[(import.LastIndexOf('.') + 1)..])).ToHashSet(StringComparer.Ordinal);
        return new(sources, qualified);
    }

    internal bool HasSource(string name) => _sources.ContainsKey(name);

    internal bool HasUniqueStream(string source, string stream) => _sources.TryGetValue(source, out var parents) && parents.Count == 1 && parents[0].Count(name => name == stream) == 1;

    internal bool HasPropertyType(string name) => _qualifiedTypes.Contains(name);

    static void SkipChildren(LineReader reader, SourceLine header)
    {
        if (header.Content.StartsWith("```", StringComparison.Ordinal))
        {
            while (reader.TakeRaw() is { } raw && raw.Raw.Trim() != "```") { }
            return;
        }
        while (reader.PeekSignificant() is { } child && child.Indent > header.Indent)
        {
            reader.TakeSignificant();
            if (child.Content.StartsWith("```", StringComparison.Ordinal))
            {
                while (reader.TakeRaw() is { } raw && raw.Raw.Trim() != "```") { }
            }
        }
    }

    [GeneratedRegex(@"^stream\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex StreamHeaderRegex();

    [GeneratedRegex(@"^eventsource\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex SourceHeaderRegex();

    [GeneratedRegex(@"^(?:type\s+([A-Za-z_]\w*)|concept\s+([A-Za-z_]\w*)\s*:.*)$", RegexOptions.None, 1000)]
    private static partial Regex TypeHeaderRegex();

    [GeneratedRegex(@"^import\s+([\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex ImportHeaderRegex();
}
