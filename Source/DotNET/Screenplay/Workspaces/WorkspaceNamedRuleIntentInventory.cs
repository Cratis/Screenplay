// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Represents one command named-rule occurrence, independently of executable readiness.
/// </summary>
/// <param name="Handle">The revision-local rule occurrence.</param>
/// <param name="Owner">The command address.</param>
/// <param name="OwnerId">The command's catalog identity.</param>
/// <param name="IdentityOrigin">The identity provenance.</param>
/// <param name="Member">The existing property/predicate member, including attached repetition discrimination.</param>
/// <param name="Hints">The ordered decoded guidance.</param>
/// <param name="File">The selected file link; inventory never opens it.</param>
/// <param name="Language">The selected inline language.</param>
/// <param name="State">Pending, file or inline selection, not execution evidence.</param>
/// <param name="RequirementId">The attachment identity, absent for pending or ambiguous owners.</param>
/// <param name="IsAmbiguous">Whether the physical command owner has colliding occurrences.</param>
public sealed record WorkspaceNamedRuleIntentEntry(
    WorkspaceNodeHandle Handle,
    SemanticAddress Owner,
    SemanticId OwnerId,
    SemanticIdentityOrigin IdentityOrigin,
    string Member,
    ImmutableArray<string> Hints,
    string? File,
    string? Language,
    string State,
    string? RequirementId,
    bool IsAmbiguous)
{
    /// <summary>
    /// Gets whether the owner and attachment identities depend on provisional legacy naming.
    /// </summary>
    public bool IsProvisional => IdentityOrigin == SemanticIdentityOrigin.LegacyBootstrap;
}

/// <summary>
/// Inventories only command property named rules with explicit intent or attached source.
/// Pending occurrences have no bound requirement identity and never advance attachment allocation.
/// </summary>
public sealed class WorkspaceNamedRuleIntentInventory
{
    WorkspaceNamedRuleIntentInventory(ImmutableArray<WorkspaceNamedRuleIntentEntry> entries, ImmutableArray<WorkspaceDocument> unresolved)
    {
        Entries = entries;
        UnresolvedPlacementDocuments = unresolved;
    }

    /// <summary>Gets the explicitly limited coverage.</summary>
    public static string Coverage => "CommandNamedRule";

    /// <summary>Gets rule occurrences in original syntax order.</summary>
    public ImmutableArray<WorkspaceNamedRuleIntentEntry> Entries { get; }

    /// <summary>Gets documents without unique physical import placement.</summary>
    public ImmutableArray<WorkspaceDocument> UnresolvedPlacementDocuments { get; }

    /// <summary>Creates a model-only inventory.</summary>
    /// <param name="workspace">The revision-bound workspace.</param>
    /// <returns>The command named-rule inventory.</returns>
    public static WorkspaceNamedRuleIntentInventory Create(ScreenplayWorkspace workspace) => Create(WorkspaceSyntaxIndex.Create(workspace));

    /// <summary>Creates a model-only inventory using an existing index.</summary>
    /// <param name="index">The revision-bound original syntax index.</param>
    /// <returns>The command named-rule inventory.</returns>
    public static WorkspaceNamedRuleIntentInventory Create(WorkspaceSyntaxIndex index)
    {
        var assignments = index.Workspace.IdentityCatalog.Semantics.ToDictionary(assignment => assignment.Address);
        var collisions = index.Entries.Where(entry => entry.Node is CommandSyntax && entry.Address is not null)
            .GroupBy(entry => entry.Address).Where(group => group.Skip(1).Any()).Select(group => group.Key).ToHashSet();
        var entries = ImmutableArray.CreateBuilder<WorkspaceNamedRuleIntentEntry>();
        foreach (var entry in index.Entries)
        {
            if (entry.Node is not ValidationRuleSyntax { Rule: ValidationRuleKind.Rule, Value: PathExpressionSyntax name } rule ||
                (rule.Implementation is null && rule.File is null && rule.Code is null))
            {
                continue;
            }

            var parent = entry.Parent is { } handle ? index.Find(handle) : null;
            if (parent?.Node is not DeclarativeValidateSyntax || parent.Parent is not { } commandHandle ||
                index.Find(commandHandle) is not { Node: CommandSyntax, Address: { } owner })
            {
                continue;
            }

            var assignment = assignments.GetValueOrDefault(owner) ?? new(owner, SemanticId.Create(owner), SemanticIdentityOrigin.LegacyBootstrap);
            var member = $"{rule.Property}/{name.Path}";
            var attached = rule.File is not null || rule.Code is not null;

            // Match the existing binder allocator, including attachments rejected later by property validation.
            var repeated = attached ? entries.Count(value => Equals(value.Owner, owner) && value.State != "pending" &&
                (value.Member == member || value.Member.StartsWith($"{member}#", StringComparison.Ordinal))) : 0;
            var distinctMember = repeated == 0 ? member : $"{member}#{repeated}";
            var ambiguous = collisions.Contains(owner);
            var state = rule switch
            {
                { File: not null } => "file",
                { Code: not null } => "inline",
                _ => "pending"
            };
            entries.Add(new(
                entry.Handle,
                owner,
                assignment.Id,
                assignment.Origin,
                distinctMember,
                [.. (rule.Implementation?.Hints ?? []).Select(hint => hint.Text)],
                rule.File?.Path,
                rule.Code?.Language,
                state,
                attached && !ambiguous ? ImplementationRequirementIdentity.Create(assignment.Id, SemanticImplementationRole.RulePredicate, distinctMember) : null,
                ambiguous));
        }

        return new(entries.ToImmutable(), index.UnresolvedPlacementDocuments);
    }
}
