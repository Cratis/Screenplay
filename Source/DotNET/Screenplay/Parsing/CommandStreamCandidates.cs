// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Captures declarations from a complete immutable source snapshot before commands parse.
/// Duplicates remain candidates and ordering never selects one.
/// </summary>
internal sealed class CommandStreamCandidates
{
    readonly Dictionary<string, List<string[]>> _sources;
    readonly HashSet<string> _qualifiedTypes;

    CommandStreamCandidates(Dictionary<string, List<string[]>> sources, HashSet<string> qualifiedTypes)
    {
        _sources = sources;
        _qualifiedTypes = qualifiedTypes;
    }

    internal static CommandStreamCandidates Capture(IEnumerable<IReadOnlyList<SourceLine>> documents, PlayPlacement? placement = null) =>
        Capture(documents.Select(lines => (lines, placement ?? PlayPlacement.Document)));

    internal static CommandStreamCandidates Capture(IEnumerable<(IReadOnlyList<SourceLine> Lines, PlayPlacement Placement)> documents)
    {
        var sources = new Dictionary<string, List<string[]>>(StringComparer.Ordinal);
        var types = new HashSet<string>(StringComparer.Ordinal);
        var imports = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (lines, placement) in documents)
        {
            // One noncommitting parse per document, with no stream candidates: commands retain the
            // legacy property interpretation. The real parser owns leaf/block rules (including domain,
            // imports and fences); no second indentation grammar or recursive capture is involved.
            var application = ScreenplayParser.Parse(new(new(lines)), lines, placement);
            foreach (var type in application.Types ?? []) types.Add(type.Name);
            foreach (var concept in application.Concepts) types.Add(concept.Name);
            foreach (var import in application.Imports) imports.Add(import.QualifiedName);
            foreach (var source in application.EventSources)
            {
                if (!sources.TryGetValue(source.Name, out var parents)) sources[source.Name] = parents = [];
                parents.Add([.. source.Streams.Select(stream => stream.Name)]);
            }
        }

        // A contract import alone does not reveal its kind. A real concept/composite declaration
        // establishes a viable property interpretation without broadening event/operation namespaces.
        var qualified = imports.Where(import => types.Contains(import[(import.LastIndexOf('.') + 1)..])).ToHashSet(StringComparer.Ordinal);
        return new(sources, qualified);
    }

    internal bool HasSource(string name) => _sources.ContainsKey(name);

    internal bool HasUniqueStream(string source, string stream) => _sources.TryGetValue(source, out var parents) && parents.Count == 1 && parents[0].Count(name => name == stream) == 1;

    internal bool HasPropertyType(string name) => _qualifiedTypes.Contains(name);
}
