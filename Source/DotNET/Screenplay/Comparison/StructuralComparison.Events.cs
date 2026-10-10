// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Comparison;

internal static partial class StructuralComparison
{
    static void Events(string? id, string previous, string next, EventSyntax[] oldNodes, EventSyntax[] newNodes, List<Change> changes, HashSet<string> changedIds)
    {
        if (oldNodes.Select(node => node.Generation).Distinct().Count() != oldNodes.Length || newNodes.Select(node => node.Generation).Distinct().Count() != newNodes.Length || oldNodes.Concat(newNodes).Any(node => node.Properties.GroupBy(property => property.Name, StringComparer.Ordinal).Any(group => group.Count() > 1))) return;
        var leftEvents = oldNodes.OrderBy(node => node.Generation).ToArray();
        var rightEvents = newNodes.OrderBy(node => node.Generation).ToArray();
        if (leftEvents.Length == 0 || rightEvents.Length == 0) return;
        foreach (var old in leftEvents)
        {
            var current = rightEvents.SingleOrDefault(node => node.Generation == old.Generation);
            if (current is not null)
            {
                Compare(old, current, false);
            }
            else
            {
                changes.Add(new("events", "generation-removed", id, "Event", previous, next, ContractBreaking: true, GenerationCovered: false, BeforeGeneration: old.Generation));
                if (id is not null) changedIds.Add(id);
            }
        }
        foreach (var current in rightEvents.Where(node => !leftEvents.Any(old => old.Generation == node.Generation)))
        {
            var previousGeneration = rightEvents.LastOrDefault(node => node.Generation < current.Generation);
            var baseline = previousGeneration is null ? null : leftEvents.SingleOrDefault(node => node.Generation == previousGeneration.Generation);
            var covered = previousGeneration is not null && current.HasGenerationMarker && (baseline is null || Shape(baseline) == Shape(previousGeneration));
            changes.Add(new("events", "generation-added", id, "Event", previous, next, GenerationCovered: covered, BeforeGeneration: previousGeneration?.Generation, AfterGeneration: current.Generation));
            if (id is not null) changedIds.Add(id);
            if (previousGeneration is not null) Compare(previousGeneration, current, covered);
        }

        void Compare(EventSyntax old, EventSyntax current, bool covered)
        {
            var left = old.Properties.ToDictionary(property => property.Name, property => SyntaxJson.Serialize(property.Type).GetRawText(), StringComparer.Ordinal);
            var right = current.Properties.ToDictionary(property => property.Name, property => SyntaxJson.Serialize(property.Type).GetRawText(), StringComparer.Ordinal);
            foreach (var property in left.Keys.Union(right.Keys).Where(property => left.GetValueOrDefault(property) != right.GetValueOrDefault(property)).Order(StringComparer.Ordinal))
            {
                var change = (left.ContainsKey(property), right.ContainsKey(property)) switch
                {
                    (false, _) => "property-added",
                    (_, false) => "property-removed",
                    _ => "property-type-changed"
                };
                changes.Add(new("events", change, id, "Event", previous, next, property, BeforeType: left.GetValueOrDefault(property), AfterType: right.GetValueOrDefault(property), ContractBreaking: true, GenerationCovered: covered, BeforeGeneration: old.Generation, AfterGeneration: current.Generation));
                if (id is not null) changedIds.Add(id);
            }
        }
    }

    static string Shape(EventSyntax node) => string.Join('|', node.Properties.OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => $"{property.Name}:{SyntaxJson.Serialize(property.Type).GetRawText()}"));
}
