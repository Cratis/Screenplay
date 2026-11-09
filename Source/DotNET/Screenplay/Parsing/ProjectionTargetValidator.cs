// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks explicit projection targets against declared read-model and element shapes.
/// </summary>
internal static class ProjectionTargetValidator
{
    /// <summary>Validates targets whose declared shapes are known.</summary>
    public static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var projection in slice.Projections)
            {
                var variants = projection.Blocks.OfType<ProjectionVariantSyntax>().ToList();
                var reported = new HashSet<SourceLocation>();
                if (variants.Count == 0)
                {
                    Walk(projection.Blocks, declarations.ViewProperties(projection.ReadModel ?? projection.Name, scope), declarations, scope, context, reported);
                }

                foreach (var variant in variants)
                {
                    // Shared top-level mappings already have PLAY0383 validation for each variant.
                    var properties = declarations.ViewProperties(variant.Name, scope);
                    Walk(projection.Blocks.Where(block => block is ChildrenSyntax or NestedSyntax), properties, declarations, scope, context, reported);

                    // Lowering combines shared handlers with variant-local blocks and turns entering events into From handlers.
                    var ownsEvents = variant.EntersOn.Any() || projection.Blocks.Any(block => block is FromSyntax or JoinSyntax or AllSyntax);
                    Walk(variant.Blocks, properties, declarations, scope, context, reported, ownsEvents: ownsEvents);
                }
            }
        }
    }

    static void Walk(IEnumerable<ProjectionBlockSyntax> blocks, IEnumerable<PropertySyntax>? properties, ConsistencyDeclarations declarations, DeclarationScope scope, ParserContext context, HashSet<SourceLocation> reported, bool element = false, bool ownsEvents = false)
    {
        // An 'every' with no effective handlers at this level only cascades into the element scopes.
        ownsEvents |= blocks.Any(block => block is FromSyntax or JoinSyntax or AllSyntax);
        foreach (var block in blocks)
        {
            var mappings = block switch
            {
                FromSyntax from => from.Mappings,
                EverySyntax every when ownsEvents => every.Mappings,
                AllSyntax all => all.Mappings,
                JoinSyntax join => join.Events.SelectMany(entry => entry.Mappings),
                _ => []
            };
            foreach (var mapping in mappings)
            {
                // A dynamic key names a dictionary entry, not a declared property: the property it writes into is what must exist.
                var basePath = EventContextPathValidator.BaseProperty(mapping.Property);
                var target = Validate(basePath, mapping.Location);

                // Chronicle accepts a dictionary entry only in an untyped or empty object property, and every declared Screenplay
                // type is a scalar, a collection or an object with properties.
                if (target is not null && EventContextPathValidator.HasDynamicKey(mapping.Property) && reported.Add(mapping.Location))
                {
                    context.Warning(
                        DiagnosticCodes.DynamicKeyOnNonDictionaryProperty,
                        $"'{mapping.Property}' writes a dictionary entry into '{basePath}', declared as '{target.Type.Name}' - Screenplay has no dictionary type, so Chronicle refuses the projection when it registers it",
                        mapping.Location);
                }
            }

            ValidateEventContentKeys(block, declarations, scope, context);

            if (block is ChildrenSyntax children)
            {
                var property = Validate(children.Property, children.Location);
                var elementProperties = property is null ? null : declarations.TypeProperties(property.Type.Name);
                if (elementProperties is not null && children.Ordering is { SortBy: { } sortBy } ordering)
                {
                    declarations.Property(elementProperties, sortBy, out var missing);
                    if (missing)
                    {
                        context.Warning(
                            DiagnosticCodes.UnknownChildrenSortProperty,
                            $"Children '{children.Property}' sorts by '{sortBy}', which is not a declared property of its element type '{property!.Type.Name}'",
                            ordering.Location);
                    }
                }

                Walk(children.Blocks, elementProperties, declarations, scope, context, reported, element: true);
            }
            else if (block is NestedSyntax nested)
            {
                var property = Validate(nested.Property, nested.Location);
                Walk(nested.Blocks, property is null ? null : declarations.TypeProperties(property.Type.Name), declarations, scope, context, reported, element: true);
            }
        }

        PropertySyntax? Validate(string path, SourceLocation location)
        {
            var property = declarations.Property(properties, path, out var missing);
            if (missing && reported.Add(location))
            {
                context.Warning(DiagnosticCodes.UnknownReadModelProperty, $"Projection target '{path}' is not a declared {(element ? "element" : "read-model")} property", location, DiagnosticArguments.Empty.With("property", DiagnosticArgument.Text(path)));
            }

            return property;
        }
    }

    static void ValidateEventContentKeys(ProjectionBlockSyntax block, ConsistencyDeclarations declarations, DeclarationScope scope, ParserContext context)
    {
        // Only a handler for named events knows which payload the key reads; 'every' and 'all' see any event.
        var handlers = block switch
        {
            FromSyntax from => from.Events.Select(spec => (spec.Event, from.Mappings)),
            JoinSyntax join => join.Events.Select(entry => (entry.Event, entry.Mappings)),
            _ => []
        };

        foreach (var (eventType, mappings) in handlers)
        {
            if (declarations.Event(eventType, scope) is not { } @event)
            {
                continue;
            }

            foreach (var mapping in mappings)
            {
                if (!EventContextPathValidator.TryGetEventContentKey(mapping.Property, out var path))
                {
                    continue;
                }

                declarations.Property(@event.Properties, path, out var missing);
                if (missing)
                {
                    context.Warning(
                        DiagnosticCodes.UnknownEventContentKey,
                        $"Dynamic key '$eventContent.{path}' in '{mapping.Property}' is not a property of event '{eventType}', so the entry is never written",
                        mapping.Location);
                }
            }
        }
    }
}
