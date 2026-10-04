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

        // Only mutual, unique matches establish unchanged occurrence correspondence. Never consume the
        // first equal duplicate, or treat an old name, source coordinate, or collection index as proof.
        Match((previous, current) => SameOwner(previous, current) && SyntaxJson.StructurallyEqual(previous.Node, current.Node) &&
            !(Bare(current.Node) && originals.Exists(other => Pending(other.Node) && SameOwner(other, current) && SameHeader(other.Node, current.Node))));

        // A renamed/moved rule may keep every item of guidance. Discharge that obligation without
        // inventing semantic identity or allocating an implementation requirement for pending intent.
        Match((previous, current) => SameOwner(previous, current) && Pending(previous.Node) &&
            (RetainsMetadata(previous.Node, current.Node) || (SameHeader(previous.Node, current.Node) && !Bare(current.Node))));

        // Across owner/header moves, unique retention of all metadata can preserve the intent without
        // proving an occurrence or owner identity. A competing bare version keeps that move ambiguous.
        Match((previous, current) => Pending(previous.Node) && RetainsMetadata(previous.Node, current.Node) &&
            !candidates.Exists(other => Bare(other.Node) && SameHeader(previous.Node, other.Node)));

        if (originals.Exists(entry => Pending(entry.Node)) && candidates.Exists(entry => Bare(entry.Node)))
        {
            throw new InvalidWorkspaceAuthoring("Pending named-rule correspondence is ambiguous. Preserve its implementation metadata or remove the original rule with a validated RemoveWorkspaceNode handle before replacing its ancestor or document.");
        }

        // Partial deletion of indistinguishable pending duplicates is not an ordinal match either.
        if (originals.Exists(entry => Pending(entry.Node) && candidates.Exists(current => SameOwner(entry, current) && RetainsMetadata(entry.Node, current.Node))))
        {
            var pending = originals.Where(entry => Pending(entry.Node)).ToArray();
            if (pending.Any(entry => candidates.Count(current => SameOwner(entry, current) && RetainsMetadata(entry.Node, current.Node)) <
                pending.Count(other => Owner(before, other)?.Handle == Owner(before, entry)?.Handle && RetainsMetadata(other.Node, entry.Node))))
            {
                throw new InvalidWorkspaceAuthoring("Deleting an ambiguous pending named-rule occurrence requires its original validated RemoveWorkspaceNode handle.");
            }
        }

        void Match(Func<WorkspaceSyntaxEntry, WorkspaceSyntaxEntry, bool> equal)
        {
            var matches = candidates.ToDictionary(current => current, current => originals.Where(previous => !sources.IsAmbiguous(current) && equal(previous, current)).ToArray());
            foreach (var (current, previous) in matches)
            {
                if (previous.Length == 1 && matches.Count(pair => pair.Value.Contains(previous[0])) == 1)
                {
                    originals.Remove(previous[0]);
                    candidates.Remove(current);
                }
            }
        }

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
