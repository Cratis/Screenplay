// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Computes source-only container identities and reports why a container cannot be targeted.
/// It never retires existing assignments or writes identity state.
/// </summary>
public sealed class WorkspaceContainerIdentities
{
    readonly WorkspaceSyntaxIndex _index;
    readonly ImmutableArray<SemanticAddress> _containers;
    readonly ImmutableHashSet<SemanticAddress> _ambiguous;

    /// <summary>
    /// Inspects the complete placed source hierarchy without requiring binding.
    /// </summary>
    /// <param name="index">The current source index.</param>
    public WorkspaceContainerIdentities(WorkspaceSyntaxIndex index)
    {
        _index = index;
        var groups = index.Entries.Where(entry => entry.Address?.Kind is SemanticKind.Module or SemanticKind.Feature or SemanticKind.Slice)
            .GroupBy(entry => entry.Address!).ToArray();
        _containers = [.. groups.Select(group => group.Key).Order(WorkspaceSemanticAddressComparer.Instance)];

        // Module and feature fragments, including placement wrappers, are one logical declaration.
        _ambiguous = [.. groups.Where(group => group.Key.Kind == SemanticKind.Slice && group.Count() > 1).Select(group => group.Key)];
    }

    /// <summary>
    /// Gets whether every document parses and has resolved placement.
    /// </summary>
    public bool Complete => _index.UnparsedDocuments.IsEmpty && _index.UnresolvedPlacementDocuments.IsEmpty;

    /// <summary>
    /// Gets containers without an identity and ambiguous containers with an existing identity.
    /// </summary>
    public ImmutableArray<WorkspaceContainerIdentityIssue> Issues => [.. _containers
        .Select(address => new WorkspaceContainerIdentityIssue(address, Reason(address), _index.Workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Equals(address))))
        .Where(issue => issue.Reason is not null)];

    /// <summary>
    /// Admits only collision-free deterministic container additions to an unbound persisted catalog.
    /// All earlier identities, origins, event revisions and document assignments must remain unchanged and present.
    /// </summary>
    /// <param name="previous">The persisted catalog.</param>
    /// <param name="workspace">The source workspace opened with that catalog.</param>
    /// <returns>Whether the exact pre-binding materialization exception applies.</returns>
    public static bool AllowsReopen(SemanticIdentityCatalog previous, ScreenplayWorkspace workspace)
    {
        if (workspace.Compilation.Success || previous.Application != workspace.IdentityCatalog.Application) return false;
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var containers = new WorkspaceContainerIdentities(index);
        var addresses = index.Entries.Where(entry => entry.Address is not null).Select(entry => entry.Address).ToHashSet();
        if (!containers.Complete || previous.Semantics.Any(assignment => !addresses.Contains(assignment.Address)) ||
            previous.EventContracts.Any(assignment => !addresses.Contains(assignment.Address)))
        {
            return false;
        }
        var expected = containers.Materialize(previous);
        return SemanticIdentityCatalogSerializer.Serialize(expected).AsSpan().SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(workspace.IdentityCatalog));
    }

    /// <summary>
    /// Tests ambiguity on the address or any container owner, including identified descendants.
    /// </summary>
    /// <param name="address">The selected address.</param>
    /// <returns>Whether an owner has multiple logical declarations.</returns>
    public bool IsAmbiguous(SemanticAddress address) => _ambiguous.Any(owner => Contains(owner, address));

    /// <summary>
    /// Reports the reason new note targeting is unavailable, without orphaning existing notes.
    /// </summary>
    /// <param name="address">The selected declaration or descendant.</param>
    /// <returns>ambiguous, idCollision or incomplete; null when targeting is available.</returns>
    public string? Reason(SemanticAddress address)
    {
        if (IsAmbiguous(address)) return "ambiguous";
        foreach (var owner in _containers.Where(owner => Contains(owner, address)))
        {
            if (_index.Workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Equals(owner))) continue;
            if (!Complete) return "incomplete";
            if (Collides(owner, _index.Workspace.IdentityCatalog)) return "idCollision";
        }

        return null;
    }

    internal SemanticIdentityCatalog Materialize(SemanticIdentityCatalog previous)
    {
        if (!Complete) return previous;
        var additions = _containers.Where(address => !previous.Semantics.Any(assignment => assignment.Address.Equals(address)) &&
                !IsAmbiguous(address) && !Collides(address, previous))
            .Select(address => new SemanticIdentityAssignment(address, SemanticId.Create(address), SemanticIdentityOrigin.LegacyBootstrap)).ToArray();

        // Match PlanSemantics' identity order so a later no-op proposal does not reorder assignments.
        return additions.Length == 0 ? previous : SemanticIdentityCatalog.Create(
            previous.Application,
            previous.Documents,
            [.. previous.Semantics.Concat(additions).OrderBy(assignment => assignment.Id.ToString(), StringComparer.Ordinal)],
            previous.EventContracts);
    }

    static bool Contains(SemanticAddress owner, SemanticAddress address) => address.Parts.Take(owner.Parts.Length).SequenceEqual(owner.Parts);

    bool Collides(SemanticAddress address, SemanticIdentityCatalog catalog)
    {
        var id = SemanticId.Create(address);
        return catalog.Semantics.Any(assignment => assignment.Id == id && !assignment.Address.Equals(address)) ||
            _containers.Any(other => !other.Equals(address) && !catalog.Semantics.Any(assignment => assignment.Address.Equals(other)) && SemanticId.Create(other) == id);
    }
}

/// <summary>
/// A source container whose identity cannot be newly targeted.
/// </summary>
/// <param name="Address">The source-indexed address.</param>
/// <param name="Reason">ambiguous, idCollision or incomplete.</param>
/// <param name="Assigned">Whether its previous assignment is retained.</param>
public sealed record WorkspaceContainerIdentityIssue(SemanticAddress Address, string? Reason, bool Assigned);
