// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks variant declarations against the known application shapes before semantic binding.
/// </summary>
/// <remarks>
/// Chronicle rejects an absent entering event in ModelBoundProjectionBuilder.cs:179-181, and rejects a shared
/// mapping whose member is absent from the variant in ModelBoundProjectionBuilder.cs:144-166. Unknown shapes
/// remain undecided rather than being assumed to contain the member (Decision: 0001).
/// </remarks>
internal static class ProjectionVariantValidator
{
    /// <summary>
    /// Validates all projections in the declaration set.
    /// </summary>
    public static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var projection in slice.Projections)
            {
                foreach (var variant in projection.Blocks.OfType<ProjectionVariantSyntax>())
                {
                    var properties = declarations.ViewProperties(variant.Name, scope);
                    foreach (var mapping in projection.Blocks.OfType<FromSyntax>().SelectMany(from => from.Mappings))
                    {
                        declarations.Property(properties, mapping.Property, out var missing);
                        if (missing)
                        {
                            context.Error(DiagnosticCodes.GlobalHandlerPropertyNotOnVariant, $"Shared handler maps '{mapping.Property}', which variant '{variant.Name}' does not declare", mapping.Location);
                        }
                    }
                }
            }
        }
    }
}
