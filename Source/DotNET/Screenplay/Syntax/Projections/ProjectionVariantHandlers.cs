// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Projections;

static class ProjectionVariantHandlers
{
    internal static IEnumerable<ProjectionBlockSyntax> For(IEnumerable<ProjectionBlockSyntax> shared, ProjectionVariantSyntax variant)
    {
        var entryNames = variant.EntersOn.Select(entry => entry.Event).ToHashSet(StringComparer.Ordinal);
        var enteringMappings = new Dictionary<string, List<MappingSyntax>>(StringComparer.Ordinal);
        foreach (var block in shared.Concat(variant.Blocks))
        {
            if (block is not FromSyntax sourceBlock)
            {
                yield return block;
                continue;
            }

            var otherEvents = new List<EventSpecSyntax>();
            foreach (var spec in sourceBlock.Events)
            {
                if (entryNames.Contains(spec.Event))
                {
                    if (!enteringMappings.TryGetValue(spec.Event, out var mappings))
                    {
                        enteringMappings[spec.Event] = mappings = [];
                    }

                    mappings.AddRange(sourceBlock.Mappings);
                }
                else
                {
                    otherEvents.Add(spec);
                }
            }

            if (otherEvents.Count > 0) yield return sourceBlock with { Events = otherEvents };
        }

        foreach (var entry in variant.EntersOn)
        {
            yield return new FromSyntax(
                [new EventSpecSyntax(entry.Event, entry.Key, entry.Location)],
                null,
                null,
                enteringMappings.GetValueOrDefault(entry.Event) ?? [],
                entry.Location);
        }
    }
}
