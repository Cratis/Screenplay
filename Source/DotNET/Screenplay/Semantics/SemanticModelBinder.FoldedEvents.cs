// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        Dictionary<string, string[]>? _foldedEventSources;

        // An outbound event projection publishes the facts it folds, so those facts' source types are the event's.
        // The sources are snapshotted before recursing, so a fact visited by a sibling recursion is still followed.
        SemanticTypeReference[] FoldedProducerTypes(string eventName, HashSet<string> visited)
        {
            var sources = FoldedEventSources(eventName).Where(source => !visited.Contains(source)).ToArray();
            return [.. sources.SelectMany(source => ProducerEventSourceTypes(source, visited))];
        }

        // Indexed once per binding pass rather than scanning every projection for every producer lookup.
        string[] FoldedEventSources(string eventName) =>
            (_foldedEventSources ??= IndexFoldedEventSources()).GetValueOrDefault(eventName, []);

        Dictionary<string, string[]> IndexFoldedEventSources()
        {
            var folded = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var projection in AllSlices().SelectMany(entry => entry.Slice.Projections))
            {
                if (PublishedEventName(projection) is not { } target) continue;
                if (!folded.TryGetValue(target, out var sources)) folded[target] = sources = [];
                sources.AddRange(projection.Blocks.SelectMany(FoldedEventNames));
            }

            return folded.ToDictionary(entry => entry.Key, entry => entry.Value.Distinct().Where(_events.ContainsKey).ToArray(), StringComparer.Ordinal);
        }

        string? PublishedEventName(ProjectionSyntax projection)
        {
            if (projection.ReadModel is null) return null;
            var target = ShortName(projection.ReadModel);
            return _readModels.ContainsKey(target) || !_events.ContainsKey(target) ? null : target;
        }

        IEnumerable<string> FoldedEventNames(ProjectionBlockSyntax block) => block switch
        {
            FromSyntax from => from.Events.Select(spec => ShortName(spec.Event)),
            EverySyntax or AllSyntax => _events.Keys,
            _ => []
        };
    }
}
