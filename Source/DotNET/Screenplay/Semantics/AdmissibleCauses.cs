// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleCauses(Func<AdmissibleDependencies> dependencies)
{
    readonly Lazy<AdmissibleDependencies> _dependencies = new(dependencies);
    readonly Dictionary<SemanticExclusionReference, SemanticExclusion> _recorded = [];
    int _round;

    internal IEnumerable<SemanticExclusion> Classify(IReadOnlyList<AdmissibleExclusions.Entry> entries)
    {
        _round++;
        var graph = _round > 1 && (entries.Any(entry => entry.Own) || _dependencies.IsValueCreated) ? _dependencies.Value : null;
        var current = entries.ToDictionary(entry => Reference(entry.Exclusion), entry => entry.Exclusion);
        var classified = entries.Select(entry => Classify(entry, current, graph)).ToArray();
        foreach (var exclusion in classified) _recorded.Add(Reference(exclusion), exclusion);
        return classified;
    }

    internal static IOrderedEnumerable<SemanticExclusion> Order(IEnumerable<SemanticExclusion> exclusions) =>
        exclusions.OrderBy(exclusion => exclusion.Location.Path, StringComparer.Ordinal)
            .ThenBy(exclusion => exclusion.Location.Line).ThenBy(exclusion => exclusion.Location.Column)
            .ThenBy(exclusion => exclusion.Kind).ThenBy(exclusion => exclusion.Address, StringComparer.Ordinal);

    SemanticExclusion Classify(AdmissibleExclusions.Entry entry, IReadOnlyDictionary<SemanticExclusionReference, SemanticExclusion> current, AdmissibleDependencies? graph)
    {
        var earlier = graph?.Of(Reference(entry.Exclusion)).Where(_recorded.ContainsKey).ToArray() ?? [];
        if (entry.Own && earlier.Length == 0) return entry.Exclusion;
        var causes = earlier.Concat(entry.ForcedBy).Distinct().ToArray();
        if (causes.Length == 0) return entry.Exclusion;
        return entry.Exclusion with
        {
            Cause = SemanticExclusionCause.Dependent,
            DependsOn = [.. Order(causes.Select(cause => _recorded.TryGetValue(cause, out var excluded) ? excluded : current[cause])).Select(Reference)]
        };
    }

    static SemanticExclusionReference Reference(SemanticExclusion exclusion) => new(exclusion.Kind, exclusion.Address);
}
