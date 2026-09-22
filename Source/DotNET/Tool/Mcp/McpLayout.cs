// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpLayout
{
    internal static ImmutableArray<WorkspaceOperation> Expand(ScreenplayWorkspace workspace, string layout = "slice")
    {
        var snapshot = new McpSnapshot(workspace.Documents);
        if (!snapshot.Compilation.Success)
        {
            throw new McpFailure("Layout expansion requires successfully parsed, merged, and resolved syntax.");
        }

        var expanded = McpLayoutDocuments.Create(snapshot.Compilation.Value!, layout).Select(file =>
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

    static ApplicationSyntax Normalize(ApplicationSyntax application) => application with
    {
        Modules = application.Modules.OrderBy(module => module.Name, StringComparer.Ordinal).Select(module => module with { Features = NormalizeFeatures(module.Features) })
    };

    static IEnumerable<FeatureSyntax> NormalizeFeatures(IEnumerable<FeatureSyntax> features) => features.OrderBy(feature => feature.Name, StringComparer.Ordinal).Select(feature => feature with
    {
        Features = NormalizeFeatures(feature.Features),
        Slices = feature.Slices.OrderBy(slice => slice.Name, StringComparer.Ordinal)
    });
}
