// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        IEnumerable<SemanticProjection> BindProjections(SemanticAddress slice, ProjectionSyntax projection)
        {
            var variants = projection.Blocks.OfType<ProjectionVariantSyntax>().ToArray();
            if (variants.Length == 0)
            {
                if (BindProjection(slice, projection) is { } ordinary)
                {
                    yield return ordinary;
                }

                yield break;
            }

            if (projection.Sequence is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{projection.Name}' sequence is not portable ESM v1 behavior.", projection.Location);
            }

            var shared = projection.Blocks.Where(block => block is not ProjectionVariantSyntax).ToArray();
            var entering = variants.SelectMany(variant => variant.EntersOn.Select(entry => (variant.Name, Entry: entry))).ToArray();
            foreach (var variant in variants)
            {
                if (!_readModels.TryGetValue(ShortName(variant.Name), out var readModel))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Variant '{variant.Name}' read model is unresolved.", variant.Location);
                    continue;
                }

                var identifier = readModel.Model.Properties.FirstOrDefault(property => property.IsIdentifier);
                if (identifier is null)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Variant '{variant.Name}' needs a read-model identifier for update-only joins.", variant.Location);
                    continue;
                }

                var variantProjectionName = $"{projection.Name.Length}:{projection.Name}:{variant.Name}";
                var address = SemanticAddress.ForProjection(slice, variantProjectionName);
                var id = Resolve(address, variant.Location);
                var level = new ProjectionLevel(variant.Name, readModel.Properties, projection.AutoMap != AutoMapMode.Disabled, true, false, identifier.Name);

                // Chronicle ModelBoundProjectionBuilder.cs:144-166 merges global handlers before VariantReclassifier.cs:28-50
                // reclassifies every non-entering From as a self-referential, update-only join (Decision: 0001).
                var blocks = shared.Concat(variant.Blocks).ToArray();
                var entryNames = variant.EntersOn.Select(entry => entry.Event).ToHashSet(StringComparer.Ordinal);
                var enteringMappings = new Dictionary<string, List<MappingSyntax>>(StringComparer.Ordinal);
                var ordinaryBlocks = new List<ProjectionBlockSyntax>();
                foreach (var block in blocks)
                {
                    if (block is not FromSyntax sourceBlock)
                    {
                        ordinaryBlocks.Add(block);
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

                    if (otherEvents.Count > 0)
                    {
                        ordinaryBlocks.Add(sourceBlock with { Events = otherEvents });
                    }
                }

                foreach (var entry in variant.EntersOn)
                {
                    ordinaryBlocks.Add(new FromSyntax(
                        [new EventSpecSyntax(entry.Event, entry.Key, entry.Location)],
                        null,
                        null,
                        enteringMappings.GetValueOrDefault(entry.Event) ?? [],
                        entry.Location));
                }

                var scope = BindScope(ordinaryBlocks, level, projection.AutoMap);
                var from = scope.From.Where(item => entryNames.Contains(EventName(item.EventContract))).ToImmutableArray();
                var joins = scope.Joins.ToBuilder();
                foreach (var transition in scope.From.Where(item => !entryNames.Contains(EventName(item.EventContract))))
                {
                    // Chronicle VariantReclassifier.cs:35-48: On is the variant's own key, and Key is the
                    // original From key (or event source identity). Joins are update-only and may match many.
                    joins.Add(new(transition.EventContract, identifier.Id, transition.Mappings) { Key = transition.Key });
                }

                var removals = scope.Removals.ToBuilder();
                foreach (var sibling in entering.Where(item => item.Name != variant.Name))
                {
                    if (_events.TryGetValue(sibling.Entry.Event, out var @event) &&
                        !removals.Any(item => item.EventContract == @event.Contract.Id))
                    {
                        // Chronicle VariantReclassifier.cs:55-72: sibling entry removes by event source identity.
                        removals.Add(new(@event.Contract.Id, SemanticProjectionKey.EventSourceIdentity, null));
                    }
                }

                // Root removal has no parent: Chronicle's ParentKey is ignored at this level.
                yield return new(id, variantProjectionName, readModel.Model.Id, [])
                {
                    Scope = scope with { From = from, Joins = joins.ToImmutable(), Removals = removals.ToImmutable() }
                };
            }
        }

        string EventName(SemanticId id) => _events.Values.FirstOrDefault(value => value.Contract.Id == id)?.Contract.Name ?? string.Empty;
    }
}
