// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpLayout
{
    internal static ImmutableArray<WorkspaceOperation> Expand(ScreenplayWorkspace workspace, string layout = "slice")
    {
        var snapshot = new McpSnapshot(workspace.Documents);
        if (!snapshot.Compilation.Success)
        {
            throw new McpFailure("Layout expansion requires successfully parsed, merged, and resolved syntax.");
        }

        var expanded = McpLayoutDocuments.Create(InPresentationOrder(snapshot.Compilation.Value!, workspace.Documents), layout).Select(file =>
            WorkspaceDocument.Create(McpDocumentKeys.For(file.RelativePath.Replace('\\', '/')), PortablePlayPath.Parse(file.RelativePath.Replace('\\', '/')), Encoding.UTF8.GetBytes(file.Content))).ToImmutableArray();
        McpRoot.CheckDocuments(expanded);
        var expandedSyntax = new McpSnapshot(expanded).Compilation;
        if (!expandedSyntax.Success || !SyntaxJson.StructurallyEqual(Normalize(snapshot.Compilation.Value!), Normalize(expandedSyntax.Value!)))
        {
            throw new McpFailure("Layout expansion failed the syntax round-trip check; no proposal was created.");
        }

        var existing = workspace.Documents.ToDictionary(document => document.Path.Value, StringComparer.Ordinal);
        var newPaths = expanded.Select(document => document.Path.Value).ToHashSet(StringComparer.Ordinal);
        var operations = ImmutableArray.CreateBuilder<WorkspaceOperation>();
        foreach (var document in expanded)
        {
            operations.Add(existing.TryGetValue(document.Path.Value, out var previous)
                ? new ReplaceWorkspaceDocument { Document = previous.Id, Bytes = document.Bytes }
                : new AddWorkspaceDocument { StableKey = document.StableKey, Path = document.Path, Bytes = document.Bytes });
        }

        operations.AddRange(workspace.Documents.Where(document => !newPaths.Contains(document.Path.Value)).Select(document => new RemoveWorkspaceDocument { Document = document.Id }));
        return operations.ToImmutable();
    }

    // Layouts preserve the board's authored sibling order without changing path-ordered compilation.
    static ApplicationSyntax InPresentationOrder(ApplicationSyntax application, ImmutableArray<WorkspaceDocument> documents)
    {
        var languages = ScreenplayLanguageRegistry.Default;
        var texts = documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal)
            .ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var (placed, _) = PlayImports.Resolve(texts.Keys, new InMemoryPlayDocumentSource(texts), languages);
        var imports = placed.ToDictionary(document => document.Path, document => ScreenplayCompiler.DiscoverImports(document.Source, document.Path, languages), StringComparer.Ordinal);
        var root = imports.TryGetValue("application.play", out var applicationImports) && applicationImports.Count == 0
            ? null
            : OrderingRoot.Select([.. texts.Keys], placed, languages, imports);
        if (root is null) return application;
        var ranks = AuthoredOrder.Record([root], placed, languages, imports: imports);

        IEnumerable<T> Ordered<T>(IEnumerable<T> items, string[] scope, Func<T, string> name) => items
            .OrderBy(item => ranks.GetValueOrDefault(AuthoredOrder.Key(scope.Append(name(item))), int.MaxValue));
        FeatureSyntax Feature(FeatureSyntax feature, string[] outer)
        {
            string[] scope = [.. outer, feature.Name];
            return feature with
            {
                Features = [.. Ordered(feature.Features, scope, child => child.Name).Select(child => Feature(child, scope))],
                Slices = [.. Ordered(feature.Slices, scope, slice => slice.Name)]
            };
        }

        return application with
        {
            Modules = [.. Ordered(application.Modules, [], module => module.Name).Select(module => module with
            {
                Features = [.. Ordered(module.Features, [module.Name], feature => feature.Name).Select(feature => Feature(feature, [module.Name]))]
            })]
        };
    }

    static ApplicationSyntax Normalize(ApplicationSyntax application) => application with
    {
        FileImports = [],
        Modules = application.Modules.OrderBy(module => module.Name, StringComparer.Ordinal).Select(module => module with { FileImports = [], Features = NormalizeFeatures(module.Features) })
    };

    static IEnumerable<FeatureSyntax> NormalizeFeatures(IEnumerable<FeatureSyntax> features) => features.OrderBy(feature => feature.Name, StringComparer.Ordinal).Select(feature => feature with
    {
        FileImports = [],
        Features = NormalizeFeatures(feature.Features),
        Slices = feature.Slices.OrderBy(slice => slice.Name, StringComparer.Ordinal)
    });
}
