// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
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

        if (!KeepsAuthoredOrder(Timeline(workspace.Documents), Timeline(expanded)))
        {
            throw new McpFailure("Layout expansion would change authored module, feature, or slice order; no proposal was created.");
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

    internal static bool KeepsAuthoredOrder(ScreenplayWorkspace before, ScreenplayWorkspace after) =>
        KeepsAuthoredOrder(Timeline(before.Documents), Timeline(after.Documents));

    static AuthoredTimeline Timeline(ImmutableArray<WorkspaceDocument> documents)
    {
        // Unranked siblings must tie-break in the same path order as InPresentationOrder's snapshot.
        var texts = documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal)
            .ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        PlayApplicationAssembly.Compile(new ScreenplayCompiler(), texts.Keys, new InMemoryPlayDocumentSource(texts), ScreenplayLanguageRegistry.Default, out var timeline);

        return timeline;
    }

    static bool KeepsAuthoredOrder(AuthoredTimeline before, AuthoredTimeline after)
    {
        if (before.Root is null) return true;
        if (after.Root is null || before.Application is null || after.Application is null) return false;
        var previous = SiblingSequences(before);
        var current = SiblingSequences(after);

        return previous.Count == current.Count && previous.All(sequence =>
            current.TryGetValue(sequence.Key, out var children) && sequence.Value.SequenceEqual(children));
    }

    // Global ranks can shift when containers move between files. Compare only each collection's
    // relative sibling order. A split container ranks at its owner file: the root if it declares the
    // container, otherwise a declaring file named after the container or an ancestor (outermost wins),
    // otherwise its first declaration in import order. Repeated sibling names count once.
    static Dictionary<string, string[]> SiblingSequences(AuthoredTimeline timeline)
    {
        var sequences = new Dictionary<string, string[]>(StringComparer.Ordinal);
        void Add(string kind, string[] scope, IEnumerable<string> names)
        {
            var children = names.Distinct(StringComparer.Ordinal).Select(name => AuthoredOrder.Key(scope.Append(name)))
                .OrderBy(key => timeline.Ranks.GetValueOrDefault(key, int.MaxValue));
            sequences.Add($"{kind}:{AuthoredOrder.Key(scope)}", [.. children]);
        }

        void Features(IEnumerable<FeatureSyntax> features, string[] scope)
        {
            var items = features.ToArray();
            Add("features", scope, items.Select(feature => feature.Name));
            foreach (var group in items.GroupBy(feature => feature.Name, StringComparer.Ordinal))
            {
                string[] child = [.. scope, group.Key];
                Add("slices", child, group.SelectMany(feature => feature.Slices).Select(slice => slice.Name));
                Features(group.SelectMany(feature => feature.Features), child);
            }
        }

        var modules = timeline.Application!.Modules.ToArray();
        Add("modules", [], modules.Select(module => module.Name));
        foreach (var group in modules.GroupBy(module => module.Name, StringComparer.Ordinal)) Features(group.SelectMany(module => module.Features), [group.Key]);

        return sequences;
    }

    // Layouts preserve the timeline's authored sibling order without changing path-ordered compilation.
    static ApplicationSyntax InPresentationOrder(ApplicationSyntax application, ImmutableArray<WorkspaceDocument> documents)
    {
        var languages = ScreenplayLanguageRegistry.Default;
        var texts = documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal)
            .ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var (placed, _) = PlayImports.Resolve(texts.Keys, new InMemoryPlayDocumentSource(texts), languages);
        var imports = placed.ToDictionary(document => document.Path, document => ScreenplayCompiler.DiscoverImports(document.Source, document.Path, languages), StringComparer.Ordinal);
        var root = OrderingRoot.Select([.. texts.Keys], placed, languages, imports);
        if (root is null) return application;
        var ranks = AuthoredOrder.Record([root], placed, languages, out var origins, imports: imports);

        SourceLocation? Position(string[] scope, SyntaxNode owner) => origins.GetValueOrDefault(AuthoredOrder.Key(scope))?
            .LastOrDefault(step => step.Node is FileImportSyntax && step.Path == owner.Location.Path)?.Location;

        IEnumerable<T> Ordered<T>(IEnumerable<T> items, string[] scope, Func<T, string> name) => items
            .OrderBy(item => ranks.GetValueOrDefault(AuthoredOrder.Key(scope.Append(name(item))), int.MaxValue));
        FeatureSyntax Feature(FeatureSyntax feature, string[] outer, SyntaxNode owner)
        {
            string[] scope = [.. outer, feature.Name];
            return feature with
            {
                PrintingLocation = Position(scope, owner),
                Features = [.. Ordered(feature.Features, scope, child => child.Name).Select(child => Feature(child, scope, feature))],
                Slices = [.. Ordered(feature.Slices, scope, slice => slice.Name).Select(slice => slice with
                {
                    PrintingLocation = Position([.. scope, slice.Name], feature)
                })]
            };
        }

        return application with
        {
            Modules = [.. Ordered(application.Modules, [], module => module.Name).Select(module => module with
            {
                Features = [.. Ordered(module.Features, [module.Name], feature => feature.Name).Select(feature => Feature(feature, [module.Name], module))]
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
