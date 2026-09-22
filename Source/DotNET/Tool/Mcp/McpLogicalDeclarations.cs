// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpLogicalDeclarations
{
    internal static void Complete(List<McpDeclaration> declarations, ApplicationSyntax application)
    {
        var positions = declarations.Select((declaration, index) => (declaration, index))
            .Where(item => item.declaration.Kind == "Module" || item.declaration.Kind == "Feature")
            .GroupBy(item => (item.declaration.Kind, item.declaration.Address))
            .ToDictionary(group => group.Key, group => group.First().index);
        foreach (var module in application.Modules)
        {
            Replace(declarations, positions, "Module", module.Name, module, module.Description);
            Features(declarations, positions, module.Features, module.Name);
        }
    }

    static void Features(List<McpDeclaration> declarations, IReadOnlyDictionary<(string Kind, string Address), int> positions, IEnumerable<FeatureSyntax> features, string parent)
    {
        foreach (var feature in features)
        {
            var address = $"{parent}.{feature.Name}";
            Replace(declarations, positions, "Feature", address, feature, feature.Description);
            Features(declarations, positions, feature.Features, address);
        }
    }

    static void Replace(List<McpDeclaration> declarations, IReadOnlyDictionary<(string Kind, string Address), int> positions, string kind, string address, SyntaxNode syntax, string? description)
    {
        if (positions.TryGetValue((kind, address), out var position))
        {
            // A record copy keeps the original fragment list and locations while exposing the merged meaning.
            declarations[position] = declarations[position] with { Syntax = syntax, Description = description };
        }
    }
}
