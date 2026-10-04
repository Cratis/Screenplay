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
    internal static void Validate(WorkspaceSyntaxIndex before, WorkspaceSyntaxIndex after, WorkspaceEditProvenance provenance)
    {
        foreach (var original in before.Entries.Where(entry => Pending(entry.Node)))
        {
            if (provenance.Image(original) is { } image && after.Entries.SingleOrDefault(entry => Position(entry) == image) is { } survivor)
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
        WorkspaceEditProvenance provenance,
        IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations)
    {
        var originals = Under(before, original).Where(entry => entry.Node is ValidationRuleSyntax && provenance.Image(entry) is null).ToList();
        var candidates = Under(after, candidate).Where(entry => entry.Node is ValidationRuleSyntax && provenance.Origin(entry) is null).ToList();

        // First account for unchanged occurrences, including pre-existing bare legacy rules. A deleted
        // pending duplicate must not be confused with an unchanged bare sibling at its former index.
        foreach (var current in candidates.ToArray())
        {
            var previous = originals.Find(entry => SameOwner(entry, current) && SyntaxJson.StructurallyEqual(entry.Node, current.Node));
            if (previous is not null)
            {
                originals.Remove(previous);
                candidates.Remove(current);
            }
        }

        foreach (var current in candidates.Where(entry => entry.Node is ValidationRuleSyntax { Implementation: null, File: null, Code: null }))
        {
            var rule = (ValidationRuleSyntax)current.Node;
            if (originals.Exists(entry => Pending(entry.Node) && SameOwner(entry, current) && entry.Node is ValidationRuleSyntax prior &&
                prior.Property == rule.Property && prior.Value is { } previousValue && rule.Value is { } value && SyntaxJson.StructurallyEqual(previousValue, value)))
            {
                RequireIntent(current.Node);
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

            // A replaced command itself is explicit correspondence even when its name changes. This
            // also covers a corresponding owner already carried by an enclosing typed replacement.
            return previousOwner is not null && currentOwner is not null &&
                ((previousOwner.Handle == original && currentOwner.Handle == candidate) || provenance.Image(previousOwner) == Position(currentOwner));
        }
    }

    static bool Pending(SyntaxNode node) => node is ValidationRuleSyntax { Implementation: not null, File: null, Code: null };

    static void RequireIntent(SyntaxNode node)
    {
        if (node is ValidationRuleSyntax { Implementation: null, File: null, Code: null })
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
