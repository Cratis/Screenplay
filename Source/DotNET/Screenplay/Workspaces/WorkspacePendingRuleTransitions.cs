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
    internal static void Validate(WorkspaceSyntaxIndex before, WorkspaceSyntaxIndex after, WorkspaceEditProvenance sources, IReadOnlySet<WorkspaceNodeHandle> removals)
    {
        foreach (var original in before.Entries.Where(entry => Pending(entry.Node) && !removals.Contains(entry.Handle)))
        {
            if (sources.Image(original) is { } image && after.Entries.SingleOrDefault(entry => Position(entry) == image) is { } survivor)
            {
                RequireIntent(survivor.Node);
            }
        }
    }

    internal static void Region(
        WorkspaceSyntaxIndex before,
        WorkspaceNodeHandle original,
        WorkspaceSyntaxIndex after,
        WorkspaceNodeHandle candidate,
        WorkspaceEditProvenance sources,
        WorkspaceEditProvenance provenance,
        IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations,
        IReadOnlySet<WorkspaceNodeHandle> removals)
    {
        var originals = Under(before, original).Where(entry => entry.Node is ValidationRuleSyntax && sources.Image(entry) is null && !removals.Contains(entry.Handle)).ToList();
        var candidates = Under(after, candidate).Where(entry => entry.Node is ValidationRuleSyntax && sources.Origin(entry) is null).ToList();

        // Only mutual, unique structural matches prove unchanged occurrences. Along with operation
        // provenance, these can prove absence: every final rule belongs to an unchanged original and
        // no unclaimed candidate can be a transformation of the missing pending rules. Coordinates,
        // collection ordinals and failed header/hint matching are never evidence of deletion.
        Match((previous, current) => SameOwner(previous, current) && SyntaxJson.StructurallyEqual(previous.Node, current.Node));
        if (candidates.Count == 0)
        {
            return;
        }

        // From here on, matches conserve obligations, not occurrence identity. Prefer the original
        // member over copied guidance, and apply the competing-bare safeguard at every strength.
        Match((previous, current) => Pending(previous.Node) && SameOwner(previous, current) && SameHeader(previous.Node, current.Node) && !Bare(current.Node));
        Match((previous, current) => Pending(previous.Node) && SameOwner(previous, current) && RetainsMetadata(previous.Node, current.Node));
        Match((previous, current) => Pending(previous.Node) && RetainsMetadata(previous.Node, current.Node));

        // Names and hints may both change, including on reordered equal pending predicates. Conserve
        // their multiplicity within the proven owner, without assigning pending IDs or claiming an
        // occurrence match. A bare competitor makes that otherwise unconstrained edit ambiguous.
        var conserved = new Dictionary<WorkspaceSyntaxEntry, WorkspaceSyntaxEntry>();
        foreach (var previous in originals.Where(entry => Pending(entry.Node)))
        {
            if (!Conserve(previous, []))
            {
                throw new InvalidWorkspaceAuthoring("Pending named-rule correspondence is ambiguous. Preserve its implementation metadata or remove the original rule with a validated RemoveWorkspaceNode handle before replacing its ancestor or document.");
            }
        }

        bool Conserve(WorkspaceSyntaxEntry previous, HashSet<WorkspaceSyntaxEntry> visited)
        {
            foreach (var current in candidates.Where(entry => !Bare(entry.Node) && SameOwner(previous, entry) && Safe(previous, entry) &&
                !candidates.Exists(other => Bare(other.Node))))
            {
                if (visited.Add(current) && (!conserved.TryGetValue(current, out var occupant) || Conserve(occupant, visited)))
                {
                    conserved[current] = previous;
                    return true;
                }
            }

            return false;
        }

        void Match(Func<WorkspaceSyntaxEntry, WorkspaceSyntaxEntry, bool> equal)
        {
            var matches = candidates.ToDictionary(current => current, current => originals.Where(previous => Safe(previous, current) && equal(previous, current)).ToArray());
            foreach (var (current, previous) in matches)
            {
                if (previous.Length == 1 && matches.Count(pair => pair.Value.Contains(previous[0])) == 1)
                {
                    originals.Remove(previous[0]);
                    candidates.Remove(current);
                }
            }
        }

        bool Safe(WorkspaceSyntaxEntry previous, WorkspaceSyntaxEntry current) => !sources.IsAmbiguous(current) &&
            !(Bare(current.Node) && originals.Exists(other => Pending(other.Node) && SameHeader(other.Node, current.Node))) &&
            !(Pending(previous.Node) && candidates.Exists(other => Bare(other.Node) && SameHeader(previous.Node, other.Node)));

        bool SameOwner(WorkspaceSyntaxEntry previous, WorkspaceSyntaxEntry current)
        {
            var previousOwner = Owner(before, previous);
            var currentOwner = Owner(after, current);
            if (previousOwner?.Address is { } address && Equals(migrations.GetValueOrDefault(address) ?? address, currentOwner?.Address))
            {
                return true;
            }

            return previousOwner is not null && currentOwner is not null &&
                ((previousOwner.Handle == original && currentOwner.Handle == candidate) || provenance.Image(previousOwner) == Position(currentOwner));
        }
    }

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
    static IEnumerable<WorkspaceSyntaxEntry> Under(WorkspaceSyntaxIndex index, WorkspaceNodeHandle root) => index.Entries.Where(entry => WorkspaceAstEdits.Contains(root, entry.Handle));

    static WorkspaceSyntaxEntry? Owner(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry entry)
    {
        for (var current = entry; current is not null; current = current.Parent is { } parent ? index.Find(parent) : null)
        {
            if (current.Node is CommandSyntax) return current;
        }

        return null;
    }
}
