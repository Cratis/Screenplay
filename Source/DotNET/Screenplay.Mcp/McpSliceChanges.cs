// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Indexing;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Projects the per-slice summary of a semantic diff into the MCP result.
/// </summary>
static class McpSliceChanges
{
    internal static object Project(StructuralDifference difference, AuthoringSnapshot before, AuthoringSnapshot after, bool matchByAddress)
    {
        var matching = matchByAddress ? DeclarationMatching.Address : DeclarationMatching.Identity;
        var summary = SliceChanges.Summarize(difference, before, after, change => ModelDifferenceProjection.Declaration(change, matching));
        return new
        {
            slices = summary.Slices.Select(slice => new
            {
                change = Name(slice.Change),
                changes = slice.Changes.Select(Name).ToArray(),
                beforeAddress = slice.BeforeAddress,
                afterAddress = slice.AfterAddress,
                semanticId = slice.SemanticId?.ToString(),
                declarations = slice.Declarations.Select(Declaration).ToArray(),
                dependentSlices = slice.DependentSlices
            }).ToArray(),
            modelWide = summary.ModelWide.Select(Declaration).ToArray(),
            affectedSlices = summary.AffectedSlices
        };
    }

    static object Declaration(ComparedDeclaration declaration) => new
    {
        kind = declaration.Kind,
        beforeAddress = declaration.BeforeAddress,
        afterAddress = declaration.AfterAddress,
        semanticId = declaration.SemanticId?.ToString()
    };

    static string Name(SliceChangeKind kind) => JsonNamingPolicy.CamelCase.ConvertName(kind.ToString());
}
