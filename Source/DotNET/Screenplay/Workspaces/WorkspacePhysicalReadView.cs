// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.Workspaces;

// Immutable physical source facts, independent of editable syntax and merged-declaration winners.
// Failed parses retain their partial trees. Unknown placement/extent is never uniqueness evidence.
sealed record WorkspacePhysicalReadView(
    ImmutableArray<WorkspaceSyntaxEntry> Entries,
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<WorkspaceDocument> UnresolvedPlacementDocuments,
    bool IsComplete)
{
    internal bool HasResolvedPlacement(WorkspaceSyntaxEntry entry) => !UnresolvedPlacementDocuments.Any(document => document.Id == entry.Handle.Document);

    internal static WorkspacePhysicalReadView Create(ScreenplayWorkspace workspace)
    {
        var texts = workspace.Documents.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var (placed, importDiagnostics) = PlayImports.Resolve(texts.Keys, new InMemoryPlayDocumentSource(texts));
        var placements = placed.ToDictionary(document => document.Path, StringComparer.Ordinal);
        var compiler = new ScreenplayCompiler();
        var candidates = ((ICommandStreamCandidateParser)compiler).CaptureCandidates(placed.Where(document => document.IsPlacementResolved)
            .Select(document => (SourceLineSplitter.Split(document.Source, path: document.Path), document.Placement)));
        var entries = ImmutableArray.CreateBuilder<WorkspaceSyntaxEntry>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        diagnostics.AddRange(importDiagnostics);
        foreach (var document in workspace.Documents)
        {
            var placement = placements[document.Path.Value];

            // An unresolved document is read only in its literal root shape, never its guessed scope.
            var parsed = compiler.ParseWithCandidates(document.Text, document.Path.Value, placement.IsPlacementResolved ? placement.Placement : PlayPlacement.Document, candidates);
            diagnostics.AddRange(parsed.Diagnostics);
            if (parsed.Value is not null) entries.AddRange(WorkspaceSyntaxIndex.PhysicalEntries(parsed.Value, workspace, document));
        }
        var unresolved = workspace.Documents.Where(document => !placements[document.Path.Value].IsPlacementResolved).ToImmutableArray();
        var complete = unresolved.IsEmpty && !diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Code != DiagnosticCodes.AmbiguousCommandStream);
        diagnostics.AddRange(workspace.Compilation.Diagnostics);

        return new(entries.ToImmutable(), [.. diagnostics.Distinct()], unresolved, complete);
    }
}
