// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Represents model-only handler intent; attachment selection is not evidence of execution.
/// </summary>
/// <param name="Handle">The revision-local handler occurrence.</param>
/// <param name="Owner">The command's catalog address.</param>
/// <param name="OwnerId">The catalog owner's identity.</param>
/// <param name="Hints">The ordered decoded hints.</param>
/// <param name="File">The selected model file link, without reading it.</param>
/// <param name="Language">The inline language, if selected.</param>
/// <param name="State">The derived pending, file or inline state.</param>
/// <param name="RequirementId">The existing handler requirement identity encoding.</param>
/// <param name="IdentityOrigin">Whether the owner identity is authoritative or provisional.</param>
public sealed record WorkspaceImplementationEntry(
    WorkspaceNodeHandle Handle,
    SemanticAddress Owner,
    SemanticId OwnerId,
    ImmutableArray<string> Hints,
    string? File,
    string? Language,
    string State,
    string RequirementId,
    SemanticIdentityOrigin IdentityOrigin)
{
    /// <summary>
    /// Gets whether the requirement identity depends on provisional legacy naming.
    /// </summary>
    public bool IsProvisional => IdentityOrigin == SemanticIdentityOrigin.LegacyBootstrap;

    /// <summary>
    /// Gets whether multiple handler occurrences share this requirement identity.
    /// Such entries cannot be selected uniquely by requirement identity alone.
    /// </summary>
    public bool IsAmbiguous { get; init; }
}

/// <summary>
/// Inventories command handlers from syntax and catalog identities, independent of ESM binding.
/// Other implementation owners are explicitly not covered. No source files are opened.
/// </summary>
public sealed class WorkspaceImplementationInventory
{
    WorkspaceImplementationInventory(ImmutableArray<WorkspaceImplementationEntry> entries, ImmutableArray<WorkspaceDocument> unresolvedPlacementDocuments)
    {
        Entries = entries;
        UnresolvedPlacementDocuments = unresolvedPlacementDocuments;
    }

    /// <summary>
    /// Gets the supported owner role.
    /// </summary>
    public static string Coverage => "CommandHandler";

    /// <summary>
    /// Gets the handler entries in syntax occurrence order.
    /// </summary>
    public ImmutableArray<WorkspaceImplementationEntry> Entries { get; }

    /// <summary>
    /// Gets documents excluded because no unique import placement, semantic owner, or requirement identity is proven.
    /// </summary>
    public ImmutableArray<WorkspaceDocument> UnresolvedPlacementDocuments { get; }

    /// <summary>
    /// Creates a model-only inventory, including explicit pending wrappers.
    /// </summary>
    /// <param name="workspace">The revision-bound workspace.</param>
    /// <returns>The handler inventory.</returns>
    public static WorkspaceImplementationInventory Create(ScreenplayWorkspace workspace) => Create(WorkspaceSyntaxIndex.Create(workspace));

    /// <summary>
    /// Creates an inventory from an existing revision-bound syntax index.
    /// </summary>
    /// <param name="index">The original syntax index.</param>
    /// <returns>The handler inventory.</returns>
    public static WorkspaceImplementationInventory Create(WorkspaceSyntaxIndex index) => Create(index, index.Workspace.IdentityCatalog.Semantics);

    internal static WorkspaceImplementationInventory Create(WorkspaceSyntaxIndex index, IEnumerable<SemanticIdentityAssignment> catalogSemantics)
    {
        var assignments = catalogSemantics.ToDictionary(assignment => assignment.Address);
        var entries = ImmutableArray.CreateBuilder<WorkspaceImplementationEntry>();
        foreach (var entry in index.Entries)
        {
            if (entry.Node is not HandlerSyntax handler || entry.Parent is not { } parent || index.Find(parent)?.Address is not { } owner) continue;
            var assignment = assignments.GetValueOrDefault(owner) ?? new(owner, SemanticId.Create(owner), SemanticIdentityOrigin.LegacyBootstrap);
            var state = handler switch
            {
                { File: not null } => "file",
                { Code: not null } => "inline",
                _ => "pending"
            };
            entries.Add(new(
                entry.Handle,
                owner,
                assignment.Id,
                [.. (handler.Implementation?.Hints ?? []).Select(hint => hint.Text)],
                handler.File?.Path,
                handler.Code?.Language,
                state,
                ImplementationRequirementIdentity.Create(assignment.Id, SemanticImplementationRole.CommandHandler, null),
                assignment.Origin));
        }

        var ambiguous = entries.GroupBy(entry => entry.RequirementId, StringComparer.Ordinal)
            .Where(group => group.Skip(1).Any()).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);

        return new([.. entries.Select(entry => entry with { IsAmbiguous = ambiguous.Contains(entry.RequirementId) })], index.UnresolvedPlacementDocuments);
    }
}
