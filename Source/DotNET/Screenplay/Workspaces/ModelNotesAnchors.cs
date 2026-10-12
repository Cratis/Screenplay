// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Resolves notes only through current catalog identities and source ownership.
/// </summary>
public static class ModelNotesAnchors
{
    /// <summary>
    /// Resolves a current identity, never the stored lastAddress hint.
    /// </summary>
    /// <param name="anchor">The durable anchor.</param>
    /// <param name="catalog">The current identity catalog.</param>
    /// <returns>The current address, or null for an orphan.</returns>
    public static SemanticAddress? Resolve(ModelNotesAnchor anchor, SemanticIdentityCatalog catalog) => anchor.Application == true
        ? SemanticAddress.ForApplication(catalog.Application)
        : catalog.Semantics.FirstOrDefault(assignment => assignment.Id.ToString() == anchor.SemanticId)?.Address;

    /// <summary>
    /// Computes orphan and resolution states on read.
    /// </summary>
    /// <param name="document">The notes.</param>
    /// <param name="catalog">The current catalog.</param>
    /// <returns>Current items, preserving every orphan.</returns>
    public static ImmutableArray<ModelNotesItem> Items(ModelNotesDocument document, SemanticIdentityCatalog catalog) => [.. document.Notes.OrderBy(note => note.Id, StringComparer.Ordinal).Select(note =>
    {
        var address = Resolve(note.Anchor, catalog);
        var state = address is null ? "orphaned" : State(note);
        return new ModelNotesItem(note, address is null ? null : ModelNotesAddress.From(address), state, address is null ? "unknown" : null);
    })];

    /// <summary>
    /// Anchors an occurrence directly when identified, otherwise to its nearest owning container.
    /// Document-root and unresolved-placement occurrences refuse rather than guessing.
    /// </summary>
    /// <param name="index">The current source index.</param>
    /// <param name="handle">The selected occurrence.</param>
    /// <returns>The resolved stable anchor.</returns>
    /// <exception cref="InvalidModelNotes">The occurrence is unknown or has no identified owner.</exception>
    public static ModelNotesAnchor ForHandle(WorkspaceSyntaxIndex index, WorkspaceNodeHandle handle)
    {
        var entry = index.Find(handle) ?? throw new InvalidModelNotes("Unknown or stale note target.");
        if (entry.Parent is null) throw new InvalidModelNotes("Document anchors are not supported; select application explicitly.");
        if (entry.SemanticId is { } id) return new(id.ToString());
        for (var owner = entry.Parent; owner is not null; owner = index.Find(owner)?.Parent)
        {
            var candidate = index.Find(owner);
            if (candidate?.Address?.Kind == SemanticKind.Application) return new(Application: true);
            if (candidate?.Address?.Kind is SemanticKind.Slice or SemanticKind.Feature or SemanticKind.Module && candidate.SemanticId is { } ownerId)
            {
                return new(ownerId.ToString());
            }
        }

        throw new InvalidModelNotes("The selected syntax has no identified slice, feature, module or application owner.");
    }

    /// <summary>
    /// Creates one host-only finding per unresolved question, grouped by owning slice/container.
    /// Orphaned open questions remain findings with null addresses in an unscoped report.
    /// </summary>
    /// <param name="document">The notes.</param>
    /// <param name="catalog">The current catalog.</param>
    /// <param name="scope">An optional dotted module/feature/slice scope.</param>
    /// <returns>The selected open questions.</returns>
    public static ImmutableArray<ModelNotesFinding> Findings(ModelNotesDocument document, SemanticIdentityCatalog catalog, string? scope = null) =>
        [.. document.Notes.Where(note => note.Kind == "question" && note.Resolution is null).OrderBy(note => note.Id, StringComparer.Ordinal)
            .Select(note => (Note: note, Address: Owner(Resolve(note.Anchor, catalog), catalog)))
            .Where(value => InScope(value.Address, scope))
            .Select(value => new ModelNotesFinding(value.Note.Id, "info", "model-note.open-question", value.Note.Text, value.Address is null ? null : ModelNotesAddress.From(value.Address)))];

    /// <summary>
    /// Selects descendants and ancestors of a dotted hierarchy scope.
    /// </summary>
    /// <param name="address">The resolved address.</param>
    /// <param name="scope">The optional scope.</param>
    /// <returns>Whether the note participates in the scope.</returns>
    public static bool InScope(SemanticAddress? address, string? scope)
    {
        if (scope is null) return true;
        if (address is null) return false;
        var parts = address.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice).Select(part => part.Key).ToArray();
        var selected = scope.Split('.');
        return parts.Take(Math.Min(parts.Length, selected.Length)).SequenceEqual(selected.Take(Math.Min(parts.Length, selected.Length)), StringComparer.Ordinal);
    }

    static string State(ModelNote note) => note.Resolution is null ? "open" : "resolved";

    static SemanticAddress? Owner(SemanticAddress? address, SemanticIdentityCatalog catalog) => address is null ? null
        : catalog.Semantics.Where(assignment => assignment.Address.Kind is SemanticKind.Slice or SemanticKind.Feature or SemanticKind.Module or SemanticKind.Application)
            .Select(assignment => assignment.Address)
            .Where(candidate => address.Parts.Take(candidate.Parts.Length).SequenceEqual(candidate.Parts))
            .OrderByDescending(candidate => candidate.Parts.Length).FirstOrDefault() ?? SemanticAddress.ForApplication(catalog.Application);
}
