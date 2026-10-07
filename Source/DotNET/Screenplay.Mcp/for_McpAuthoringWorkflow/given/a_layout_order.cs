// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

static class a_layout_order
{
    internal static string[] Sequences(ScreenplayWorkspace workspace)
    {
        var texts = workspace.Documents.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var source = new InMemoryPlayDocumentSource(texts);
        var languages = ScreenplayLanguageRegistry.Default;
        var (placed, _) = PlayImports.Resolve(texts.Keys, source, languages);
        var root = OrderingRoot.Select([.. texts.Keys], placed, languages);
        root.ShouldNotBeNull();

        // These samples are wholly reachable from one root. Follow that root's imports and walk
        // the merged tree's printed sibling order, without using timeline ranks or the guard's comparator.
        var (_, compilation) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), [root!], source);
        compilation.Success.ShouldBeTrue();
        var application = compilation.Value!;
        var sequences = new List<string>
        {
            $"modules:[]:{JsonSerializer.Serialize(application.Modules.Select(module => module.Name))}"
        };
        void Features(IEnumerable<FeatureSyntax> features, string[] scope)
        {
            var items = features.ToArray();
            sequences.Add($"features:{JsonSerializer.Serialize(scope)}:{JsonSerializer.Serialize(items.Select(feature => feature.Name))}");
            foreach (var feature in items)
            {
                string[] child = [.. scope, feature.Name];
                sequences.Add($"slices:{JsonSerializer.Serialize(child)}:{JsonSerializer.Serialize(feature.Slices.Select(slice => slice.Name))}");
                Features(feature.Features, child);
            }
        }

        foreach (var module in application.Modules) Features(module.Features, [module.Name]);

        return [.. sequences.Order(StringComparer.Ordinal)];
    }
}
