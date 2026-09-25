// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Finds fields that no declared child or nested projection mapping can populate.
/// </summary>
internal static class ProjectionCompletenessValidator
{
    /// <summary>
    /// Validates declared element shapes, respecting inherited mappings and AutoMap.
    /// </summary>
    /// <param name="declarations">The application declarations.</param>
    /// <param name="context">The diagnostic sink.</param>
    public static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var projection in slice.Projections)
            {
                var shared = projection.Blocks.Where(block => block is not ProjectionVariantSyntax).ToList();
                var variants = projection.Blocks.OfType<ProjectionVariantSyntax>().ToList();
                if (variants.Count == 0)
                {
                    Walk(shared, declarations.ViewProperties(projection.ReadModel ?? projection.Name, scope), Enabled(projection.AutoMap, true), [], scope, declarations, context);
                }

                foreach (var variant in variants)
                {
                    Walk([.. shared, .. variant.Blocks], declarations.ViewProperties(variant.Name, scope), Enabled(projection.AutoMap, true), [], scope, declarations, context);
                }
            }
        }
    }

    static void Walk(IReadOnlyList<ProjectionBlockSyntax> blocks, IEnumerable<PropertySyntax>? properties, bool autoMap, IReadOnlyList<EverySyntax> inherited, DeclarationScope scope, ConsistencyDeclarations declarations, ParserContext context)
    {
        var cascading = inherited.Concat(blocks.OfType<EverySyntax>().Where(every => every.IncludeChildren)).ToList();
        foreach (var block in blocks)
        {
            switch (block)
            {
                case ChildrenSyntax children:
                    ValidateElement(children.Property, [.. children.Blocks], children.IdentifiedBy, Enabled(children.AutoMap, autoMap), children.Location);
                    break;
                case NestedSyntax nested:
                    ValidateElement(nested.Property, [.. nested.Blocks], null, Enabled(nested.AutoMap, autoMap), nested.Location);
                    break;
            }
        }

        void ValidateElement(string path, IReadOnlyList<ProjectionBlockSyntax> childBlocks, ExpressionSyntax? identity, bool childAutoMap, SourceLocation location)
        {
            var property = declarations.Property(properties, path, out _);
            var element = property is null ? null : declarations.TypeProperties(property.Type.Name)?.ToList();
            if (element is null)
            {
                return;
            }

            var mapped = Covered(childBlocks, element, childAutoMap, cascading, scope, declarations);
            if (mapped is not null)
            {
                if (identity is PathExpressionSyntax identifier)
                {
                    mapped.Add(Root(identifier.Path));
                }

                foreach (var missing in element.Where(field => !mapped.Contains(field.Name)))
                {
                    context.Error(
                        DiagnosticCodes.UnpopulatedProjectionField,
                        $"Projection block '{path}' never populates field '{missing.Name}' of element type '{property!.Type.Name}'",
                        location);
                }
            }

            Walk(childBlocks, element, childAutoMap, cascading, scope, declarations, context);
        }
    }

    static HashSet<string>? Covered(IReadOnlyList<ProjectionBlockSyntax> blocks, IReadOnlyList<PropertySyntax> properties, bool autoMap, IReadOnlyList<EverySyntax> inherited, DeclarationScope scope, ConsistencyDeclarations declarations)
    {
        var mapped = new HashSet<string>(StringComparer.Ordinal);
        var every = inherited.Concat(blocks.OfType<EverySyntax>()).ToList();
        foreach (var mapping in every.SelectMany(item => item.Mappings))
        {
            mapped.Add(Root(mapping.Property));
        }

        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    foreach (var mapping in from.Mappings)
                    {
                        mapped.Add(Root(mapping.Property));
                    }

                    if (autoMap || every.Exists(item => Enabled(item.AutoMap, autoMap)))
                    {
                        foreach (var source in from.Events)
                        {
                            var eventType = declarations.Event(source.Event, scope);
                            if (eventType is null)
                            {
                                return null;
                            }

                            foreach (var field in properties.Where(field => eventType.Properties.Any(sourceField =>
                                sourceField.Name == field.Name && declarations.Compatible(sourceField.Type, field.Type) != false)))
                            {
                                mapped.Add(field.Name);
                            }
                        }
                    }

                    break;
                case AllSyntax all:
                    if (Enabled(all.AutoMap, autoMap))
                    {
                        return null;
                    }

                    foreach (var mapping in all.Mappings)
                    {
                        mapped.Add(Root(mapping.Property));
                    }

                    break;
                case JoinSyntax join:
                    foreach (var joined in join.Events)
                    {
                        foreach (var mapping in joined.Mappings)
                        {
                            mapped.Add(Root(mapping.Property));
                        }

                        if (autoMap)
                        {
                            var eventType = declarations.Event(joined.Event, scope);
                            if (eventType is null)
                            {
                                return null;
                            }

                            foreach (var field in properties.Where(field => eventType.Properties.Any(sourceField =>
                                sourceField.Name == field.Name && declarations.Compatible(sourceField.Type, field.Type) != false)))
                            {
                                mapped.Add(field.Name);
                            }
                        }
                    }

                    break;
                case ChildrenSyntax children:
                    mapped.Add(Root(children.Property));
                    break;
                case NestedSyntax nested:
                    mapped.Add(Root(nested.Property));
                    break;
            }
        }

        return mapped;
    }

    static bool Enabled(AutoMapMode mode, bool inherited) => mode switch
    {
        AutoMapMode.Enabled => true,
        AutoMapMode.Disabled => false,
        _ => inherited
    };

    static string Root(string path) => path.Split('.')[0];
}
