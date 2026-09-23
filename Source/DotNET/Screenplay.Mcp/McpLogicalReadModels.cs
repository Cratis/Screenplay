// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Mcp;

static class McpLogicalReadModels
{
    internal static IEnumerable<McpDeclaration> From(IEnumerable<McpDeclaration> declarations)
    {
        var declared = declarations.ToArray();
        var explicitAddresses = declared.Where(declaration => declaration.Kind == "ReadModel").Select(declaration => declaration.Address).ToHashSet(StringComparer.Ordinal);
        var outputs = declared.SelectMany(declaration => Outputs(declaration.Syntax)
            .Select(output => new McpDeclaration("ReadModel", output.Name, declaration.Scope, output.Syntax.Location, null, null, output.Syntax) { IsImplicit = true }));

        // A shape and its builder name one view, not competing declarations. Keep the builder
        // declarations themselves intact: compilation diagnostics still report multiple builders.
        foreach (var group in outputs.GroupBy(output => output.Address, StringComparer.Ordinal)
            .Where(group => !explicitAddresses.Contains(group.Key)))
        {
            var model = group.First();
            model.Parts.AddRange(group.Skip(1).Select(output => output.Syntax));
            yield return model;
        }
    }

    static IEnumerable<(string Name, SyntaxNode Syntax)> Outputs(SyntaxNode syntax) => syntax switch
    {
        ProjectionSyntax projection when projection.Blocks.OfType<ProjectionVariantSyntax>().Any() => projection.Blocks.OfType<ProjectionVariantSyntax>().Select(variant => (variant.Name, (SyntaxNode)variant)),
        ProjectionSyntax projection => [(projection.ReadModel ?? projection.Name, projection)],
        ReducerSyntax reducer => [(reducer.ReadModel, reducer)],
        _ => []
    };
}
