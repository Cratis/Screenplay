// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>Refreshes optional attachment inputs from the trusted physical MCP root.</summary>
static class McpAttachmentContents
{
    internal static ScreenplayWorkspace Refresh(McpRoot root, ScreenplayWorkspace workspace, Request? request = null)
    {
        var loaded = Load(root, workspace.Documents, request);
        if (workspace.AttachmentContents.Count == loaded.Contents.Count &&
            workspace.AttachmentContents.All(entry => loaded.Contents.TryGetValue(entry.Key, out var value) && value == entry.Value) &&
            workspace.AttachmentDiagnostics.SequenceEqual(loaded.Diagnostics))
        {
            return workspace;
        }

        return workspace.WithAttachmentContents(loaded.Contents, loaded.Diagnostics);
    }

    internal static AttachmentFileResult Load(McpRoot root, ImmutableArray<WorkspaceDocument> source, Request? request = null)
    {
        var loaded = AttachmentFiles.LoadReferences(root.DirectoryPath, request?.References(source) ?? Discover(source, ScreenplayLanguageRegistry.Default));
        var visuals = ScreenVisualFiles.Check(root.DirectoryPath, request?.Visuals(source) ?? DiscoverVisuals(source, ScreenplayLanguageRegistry.Default));
        return visuals.IsEmpty ? loaded : loaded with { Diagnostics = [.. loaded.Diagnostics, .. visuals] };
    }

    static ImmutableArray<ScreenVisualSyntax> DiscoverVisuals(ImmutableArray<WorkspaceDocument> source, IScreenplayLanguageRegistry languages) =>
        ScreenVisualFiles.In(
            [.. source.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text))],
            languages);

    static ImmutableArray<FileReferenceSyntax> Discover(ImmutableArray<WorkspaceDocument> source, IScreenplayLanguageRegistry languages) =>
        AttachmentFiles.References(
            [.. source.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text))],
            languages);

    // Only manifests live here, never file contents or filesystem/security decisions. Production uses
    // the default registry; an isolated registry lets specs mutate configuration without global races.
    internal sealed class Request(IScreenplayLanguageRegistry? languages = null)
    {
        const int MaximumSnapshots = 4;
        readonly IScreenplayLanguageRegistry _languages = languages ?? ScreenplayLanguageRegistry.Default;
        readonly Lock _lock = new();
        readonly List<Snapshot> _snapshots = [];

        internal ImmutableArray<FileReferenceSyntax> References(ImmutableArray<WorkspaceDocument> source) => SnapshotOf(source).References;

        internal ImmutableArray<ScreenVisualSyntax> Visuals(ImmutableArray<WorkspaceDocument> source) => SnapshotOf(source).Visuals;

        Snapshot SnapshotOf(ImmutableArray<WorkspaceDocument> source)
        {
            lock (_lock)
            {
                var configuration = ParsingConfiguration();
                var existing = _snapshots.Find(snapshot => snapshot.Configuration == configuration && snapshot.Documents.SequenceEqual(source));
                if (existing is not null) return existing;

                var snapshot = new Snapshot(source, configuration, Discover(source, _languages), DiscoverVisuals(source, _languages));
                if (_snapshots.Count == MaximumSnapshots) _snapshots.RemoveAt(0);
                _snapshots.Add(snapshot);
                return snapshot;
            }
        }

        string ParsingConfiguration() => JsonSerializer.Serialize(new
        {
            Languages = _languages.InlineLanguages.Order(StringComparer.Ordinal),
            Triggers = _languages.Triggers.OrderBy(entry => entry.Key, StringComparer.Ordinal)
        });

        // WorkspaceDocument is sealed and immutable: identity equality conservatively covers exact
        // bytes, text, paths and import placement across the complete ordered document set.
        sealed record Snapshot(ImmutableArray<WorkspaceDocument> Documents, string Configuration, ImmutableArray<FileReferenceSyntax> References, ImmutableArray<ScreenVisualSyntax> Visuals);
    }
}
