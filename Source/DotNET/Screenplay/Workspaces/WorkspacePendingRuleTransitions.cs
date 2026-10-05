// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

// This is a source transition guard, not a semantic identity or an attachment allocation rule.
// Validate final, round-tripped source after the complete atomic transaction has settled.
static class WorkspacePendingRuleTransitions
{
    internal static void Validate(
        WorkspaceSyntaxIndex before,
        WorkspaceSyntaxIndex after,
        WorkspaceEditProvenance sources,
        WorkspaceEditProvenance provenance,
        IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations,
        IReadOnlySet<WorkspaceNodeHandle> removals,
        IReadOnlySet<WorkspaceNodeHandle> regions)
    {
        var finalPositions = after.Entries.ToDictionary(Position);
        var originalRules = before.Entries.Where(entry => entry.Node is ValidationRuleSyntax).ToArray();
        foreach (var original in originalRules.Where(entry => Pending(entry.Node) && !removals.Contains(entry.Handle)))
        {
            if (sources.Image(original) is { } image && finalPositions.GetValueOrDefault(image) is { } survivor)
            {
                RequireIntent(survivor.Node);
            }
        }

        // Index each final owner once. A region contributes original obligations, never a fresh pool
        // of final candidates. Handles distinguish equal rules and union overlapping original regions.
        var finalOwners = after.Entries.Where(entry => entry.Node is CommandSyntax).ToArray();
        var ownersByAddress = finalOwners.Where(entry => entry.Address is not null).GroupBy(entry => entry.Address!)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var occurrencesByOwner = after.Entries.Where(entry => entry.Node is ValidationRuleSyntax && sources.Origin(entry) is null)
            .Select(entry => (Entry: entry, Owner: Owner(after, entry))).Where(rule => rule.Owner is not null)
            .GroupBy(rule => rule.Owner!.Handle).ToDictionary(group => group.Key, group => group.Select(rule => rule.Entry).ToArray());
        var groups = originalRules.Where(entry => Affected(entry) && sources.Image(entry) is null && !removals.Contains(entry.Handle))
            .Select(entry => (Entry: entry, Owner: Owner(before, entry))).Where(rule => rule.Owner is not null)
            .GroupBy(rule => rule.Owner!.Handle).Where(group => group.Any(rule => Pending(rule.Entry.Node))).ToArray();
        var claimedOwners = new HashSet<WorkspaceNodeHandle>();
        foreach (var group in groups)
        {
            var originalOwner = before.Find(group.Key)!;
            WorkspaceSyntaxEntry? finalOwner = null;
            if (provenance.Image(originalOwner) is { } image && finalPositions.GetValueOrDefault(image) is { Node: CommandSyntax } carried)
            {
                finalOwner = carried;
            }
            else if (originalOwner.Address is { } address && ownersByAddress.TryGetValue(migrations.GetValueOrDefault(address) ?? address, out var owners))
            {
                if (owners.Length != 1)
                {
                    throw Ambiguous();
                }

                finalOwner = owners[0];
            }

            if (finalOwner is null)
            {
                continue;
            }

            // Exact command occurrences cannot share a final owner, even through an address migration.
            if (!claimedOwners.Add(finalOwner.Handle))
            {
                throw Ambiguous();
            }

            ConserveOwner([.. group.Select(rule => rule.Entry)], occurrencesByOwner.GetValueOrDefault(finalOwner.Handle, []), sources);
        }

        bool Affected(WorkspaceSyntaxEntry entry)
        {
            for (var current = entry; current is not null; current = current.Parent is { } parent ? before.Find(parent) : null)
            {
                if (regions.Contains(current.Handle)) return true;
            }

            return false;
        }
    }

    static void ConserveOwner(List<WorkspaceSyntaxEntry> originals, WorkspaceSyntaxEntry[] occurrences, WorkspaceEditProvenance sources)
    {
        // Take one stable whole-owner view before matching consumes candidates. A copied block
        // must not hide a bare survivor in another block, including outside all edit regions.
        // Validated node/member lineage has already claimed its image exactly once.
        var candidates = occurrences.ToList();
        var obligations = originals.Where(entry => Pending(entry.Node)).ToArray();
        var bare = occurrences.Where(entry => Bare(entry.Node)).ToArray();
        var competing = obligations.Where(previous => bare.Any(current => SameHeader(previous.Node, current.Node))).Select(entry => entry.Handle).ToHashSet();
        var ambiguousBare = bare.Where(current => obligations.Any(previous => SameHeader(previous.Node, current.Node))).Select(entry => entry.Handle).ToHashSet();

        // Only mutual, unique structural matches prove unchanged occurrences. Along with operation
        // provenance, these can prove absence: every final rule belongs to an unchanged original and
        // no unclaimed candidate can be a transformation of the missing pending rules. Coordinates,
        // collection ordinals and failed header/hint matching are never evidence of deletion.
        Match((previous, current) => SyntaxJson.StructurallyEqual(previous.Node, current.Node));
        if (candidates.Count == 0)
        {
            return;
        }

        // From here on, matches conserve obligations, not occurrence identity. Prefer the original
        // member over copied guidance, and apply the competing-bare safeguard at every strength.
        Match((previous, current) => Pending(previous.Node) && SameHeader(previous.Node, current.Node) && !Bare(current.Node));
        Match((previous, current) => Pending(previous.Node) && RetainsMetadata(previous.Node, current.Node));

        // Names and hints may both change, including on reordered equal pending predicates. Conserve
        // their multiplicity within the proven owner, without assigning pending IDs or claiming an
        // occurrence match. A bare competitor makes that otherwise unconstrained edit ambiguous.
        var conserved = new Dictionary<WorkspaceNodeHandle, WorkspaceSyntaxEntry>();
        foreach (var previous in originals.Where(entry => Pending(entry.Node)))
        {
            if (!Conserve(previous, []))
            {
                throw Ambiguous();
            }
        }

        bool Conserve(WorkspaceSyntaxEntry previous, HashSet<WorkspaceNodeHandle> visited)
        {
            foreach (var current in candidates.Where(entry => !Bare(entry.Node) && Safe(previous, entry) && bare.Length == 0))
            {
                if (visited.Add(current.Handle) && (!conserved.TryGetValue(current.Handle, out var occupant) || Conserve(occupant, visited)))
                {
                    conserved[current.Handle] = previous;
                    return true;
                }
            }

            return false;
        }

        void Match(Func<WorkspaceSyntaxEntry, WorkspaceSyntaxEntry, bool> equal)
        {
            var matches = candidates.ToDictionary(current => current.Handle, current => originals.Where(previous => Safe(previous, current) && equal(previous, current)).ToArray());
            var claims = matches.Values.SelectMany(entries => entries).GroupBy(entry => entry.Handle).ToDictionary(group => group.Key, group => group.Count());
            foreach (var current in candidates.ToArray())
            {
                var previous = matches[current.Handle];
                if (previous.Length == 1 && claims[previous[0].Handle] == 1)
                {
                    originals.RemoveAll(entry => entry.Handle == previous[0].Handle);
                    candidates.RemoveAll(entry => entry.Handle == current.Handle);
                }
            }
        }

        bool Safe(WorkspaceSyntaxEntry previous, WorkspaceSyntaxEntry current) => !sources.IsAmbiguous(current) &&
            !ambiguousBare.Contains(current.Handle) && !competing.Contains(previous.Handle);
    }

    static InvalidWorkspaceAuthoring Ambiguous() => new("Pending named-rule correspondence is ambiguous. Preserve its implementation metadata or remove the original rule with a validated RemoveWorkspaceNode handle before replacing its ancestor or document.");

    static bool SameHeader(SyntaxNode previous, SyntaxNode current) => previous is ValidationRuleSyntax prior && current is ValidationRuleSyntax rule &&
        prior.Property == rule.Property && prior.Rule == rule.Rule &&
        (prior.Value is null ? rule.Value is null : rule.Value is not null && SyntaxJson.StructurallyEqual(prior.Value, rule.Value));

    static bool RetainsMetadata(SyntaxNode previous, SyntaxNode current) => previous is ValidationRuleSyntax prior && current is ValidationRuleSyntax rule &&
        rule.Rule == ValidationRuleKind.Rule && prior.Implementation is not null && rule.Implementation is not null &&
        SyntaxJson.StructurallyEqual(prior.Implementation, rule.Implementation);

    static bool Pending(SyntaxNode node) => node is ValidationRuleSyntax { Implementation: not null, File: null, Code: null };
    static bool Bare(SyntaxNode node) => node is ValidationRuleSyntax { Implementation: null, File: null, Code: null };

    static void RequireIntent(SyntaxNode node)
    {
        if (Bare(node))
        {
            throw new InvalidWorkspaceAuthoring("Removing pending named-rule intent requires attaching a predicate source or removing the rule explicitly.");
        }
    }

    static (DocumentId Document, string Path) Position(WorkspaceSyntaxEntry entry) => (entry.Handle.Document, entry.Handle.Path);

    static WorkspaceSyntaxEntry? Owner(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry entry)
    {
        for (var current = entry; current is not null; current = current.Parent is { } parent ? index.Find(parent) : null)
        {
            if (current.Node is CommandSyntax) return current;
        }

        return null;
    }
}
