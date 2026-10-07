// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using static Cratis.Screenplay.Parsing.ProjectionFieldCoverage;

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
                    ValidateElement(children.Property, [.. children.Blocks], children.IdentifiedBy, Enabled(children.AutoMap, autoMap), children.Location, false);
                    break;
                case NestedSyntax nested:
                    ValidateElement(nested.Property, [.. nested.Blocks], null, Enabled(nested.AutoMap, autoMap), nested.Location, true);
                    break;
            }
        }

        void ValidateElement(string path, IReadOnlyList<ProjectionBlockSyntax> childBlocks, ExpressionSyntax? identity, bool childAutoMap, SourceLocation location, bool isNested)
        {
            var property = declarations.Property(properties, path, out _);
            var element = property is null ? null : declarations.TypeProperties(property.Type.Name)?.ToList();
            if (element is null)
            {
                return;
            }

            var mapped = Covered(childBlocks, element, childAutoMap, cascading, scope, declarations, isNested);
            if (mapped is not null)
            {
                if (identity is PathExpressionSyntax identifier)
                {
                    mapped.Add(Root(identifier.Path));
                }

                var nestedJoinNote = isNested && childBlocks.Any(block => block is JoinSyntax)
                    ? " (Chronicle does not currently apply joins inside 'nested' blocks; Cratis/Chronicle#4125)"
                    : string.Empty;
                foreach (var missing in element.Where(field => !mapped.Contains(field.Name)))
                {
                    context.Error(
                        DiagnosticCodes.UnpopulatedProjectionField,
                        $"Projection block '{path}' never populates field '{missing.Name}' of element type '{property!.Type.Name}'{nestedJoinNote}",
                        location);
                }
            }

            Walk(childBlocks, element, childAutoMap, cascading, scope, declarations, context);
        }
    }
}
