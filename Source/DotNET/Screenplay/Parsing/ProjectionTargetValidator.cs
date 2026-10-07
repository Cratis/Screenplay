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
                    Walk(projection.Blocks, declarations.ViewProperties(projection.ReadModel ?? projection.Name, scope), declarations, context, reported);
                }

                foreach (var variant in variants)
                {
                    // Shared top-level mappings already have PLAY0383 validation for each variant.
                    var properties = declarations.ViewProperties(variant.Name, scope);
                    Walk(projection.Blocks.Where(block => block is ChildrenSyntax or NestedSyntax), properties, declarations, context, reported);

                    // Lowering combines shared handlers with variant-local blocks and turns entering events into From handlers.
                    var ownsEvents = variant.EntersOn.Any() || projection.Blocks.Any(block => block is FromSyntax or JoinSyntax or AllSyntax);
                    Walk(variant.Blocks, properties, declarations, context, reported, ownsEvents: ownsEvents);
                }
            }
        }
    }

    static void Walk(IEnumerable<ProjectionBlockSyntax> blocks, IEnumerable<PropertySyntax>? properties, ConsistencyDeclarations declarations, ParserContext context, HashSet<SourceLocation> reported, bool element = false, bool ownsEvents = false)
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
                Validate(mapping.Property, mapping.Location);
            }

            if (block is ChildrenSyntax children)
            {
                var property = Validate(children.Property, children.Location);
                Walk(children.Blocks, property is null ? null : declarations.TypeProperties(property.Type.Name), declarations, context, reported, element: true);
            }
            else if (block is NestedSyntax nested)
            {
                var property = Validate(nested.Property, nested.Location);
                Walk(nested.Blocks, property is null ? null : declarations.TypeProperties(property.Type.Name), declarations, context, reported, element: true);
            }
        }

        PropertySyntax? Validate(string path, SourceLocation location)
        {
            var property = declarations.Property(properties, path, out var missing);
            if (missing && reported.Add(location))
            {
                context.Warning(DiagnosticCodes.UnknownReadModelProperty, $"Projection target '{path}' is not a declared {(element ? "element" : "read-model")} property", location);
            }

            return property;
        }
    }
}
