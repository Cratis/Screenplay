// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceAuthoringReferences
{
    internal static void Validate(ScreenplayWorkspace before, ScreenplayWorkspace after, WorkspaceAuthoringRequest request, ImmutableArray<Diagnostic>.Builder diagnostics, IReadOnlyDictionary<SemanticAddress, SemanticAddress>? referenceRenames)
    {
        var previousIndex = WorkspaceSyntaxIndex.Create(before);
        var currentIndex = WorkspaceSyntaxIndex.Create(after);
        var current = new WorkspaceReferenceBindings(currentIndex);
        if (WorkspaceReferenceLayout.Equivalent(before, after))
        {
            foreach (var binding in current.Bindings.Where(binding => binding.Target is null))
            {
                ReportDebt(binding, diagnostics);
            }

            return;
        }

        var migrations = referenceRenames?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        foreach (var rename in request.SemanticRenames)
        {
            migrations[rename.PreviousAddress] = rename.CurrentAddress;
        }
        var previousBindings = new WorkspaceReferenceBindings(previousIndex).Bindings;
        var previous = previousBindings.GroupBy(binding => Occurrence(binding.Reference, previousIndex, migrations))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var matched = new HashSet<string>(StringComparer.Ordinal);
        var introduced = new List<WorkspaceReferenceBinding>();
        foreach (var binding in current.Bindings)
        {
            var key = Occurrence(binding.Reference, currentIndex, []);
            previous.TryGetValue(key, out var matches);
            if (matches is { Length: > 1 })
            {
                throw new InvalidWorkspaceAuthoring($"Reference correspondence is not unique at '{binding.Reference.Key}'.");
            }

            var original = matches?.SingleOrDefault();
            if (original is null)
            {
                introduced.Add(binding);
            }
            else
            {
                matched.Add(original.Reference.Key);
            }

            if (original?.Target is { } resolved)
            {
                if (binding.Target is null || (original.Reference.Text == binding.Reference.Text && !SameTarget(resolved, binding.Target, migrations)))
                {
                    throw new InvalidWorkspaceAuthoring($"Existing reference '{binding.Reference.Key}' would lose its resolved target or silently rebind unchanged reference text. Edit the reference explicitly to a valid target, or supply a semantic migration for a genuine identity rename.");
                }
            }
            else if (original is not null && binding.Target is not null && original.Reference.Text == binding.Reference.Text &&
                !IsCreatedDeclaration(binding.Target, previousIndex, currentIndex, migrations))
            {
                throw new InvalidWorkspaceAuthoring($"Existing reference binding debt at '{binding.Reference.Key}' would become resolved; its intended target cannot be inferred safely.");
            }
            else if (binding.Target is null)
            {
                var unchanged = original is not null && original.Reference.Text == binding.Reference.Text && original.Outcome == binding.Outcome &&
                    SyntaxJson.StructurallyEqual(original.Reference.Entry.Node, binding.Reference.Entry.Node);
                if (!unchanged && request.ReferencePolicy == WorkspaceAuthoringReferencePolicy.Safe)
                {
                    throw new InvalidWorkspaceAuthoring($"New {binding.Outcome} {binding.Reference.Domain} reference '{binding.Reference.Text}' at '{binding.Reference.Key}'. Use Draft to retain deliberate reference debt.");
                }

                ReportDebt(binding, diagnostics);
            }
        }

        var disappeared = previousBindings.Where(binding => !matched.Contains(binding.Reference.Key)).ToArray();
        if (introduced.Count > 0 && disappeared.Length > 0)
        {
            throw new InvalidWorkspaceAuthoring("Reference occurrence correspondence cannot be proven for simultaneous removal/reordering and insertion. Split explicit removals and additions, or preserve the existing occurrence owners with semantic migrations.");
        }
    }

    static void ReportDebt(WorkspaceReferenceBinding binding, ImmutableArray<Diagnostic>.Builder diagnostics) => diagnostics.Add(Diagnostic.Warning(
        DiagnosticCodes.AmbiguousReference,
        $"Reference debt: {binding.Outcome} {binding.Reference.Domain} '{binding.Reference.Text}' at '{binding.Reference.Path}'.",
        binding.Reference.Entry.Location));

    static bool IsCreatedDeclaration(WorkspaceReferenceDeclaration target, WorkspaceSyntaxIndex before, WorkspaceSyntaxIndex after, Dictionary<SemanticAddress, SemanticAddress> migrations)
    {
        if (target.Entry?.Address is not { } address || migrations.ContainsValue(address) || before.Entries.Any(entry => address.Equals(entry.Address)))
        {
            return false;
        }

        var retained = after.Entries.Where(entry => entry.Address is not null).Select(entry => entry.Address!).ToHashSet();
        return before.Entries.Where(entry => entry.Address is not null).All(entry => retained.Contains(migrations.GetValueOrDefault(entry.Address!) ?? entry.Address!));
    }

    static bool SameTarget(WorkspaceReferenceDeclaration before, WorkspaceReferenceDeclaration after, Dictionary<SemanticAddress, SemanticAddress> migrations)
    {
        if (before.Entry?.Address is { } address)
        {
            return (migrations.GetValueOrDefault(address) ?? address).Equals(after.Entry?.Address);
        }

        return before.Key == after.Key;
    }

    static ReferenceOccurrence Occurrence(WorkspaceReferenceMember reference, WorkspaceSyntaxIndex index, Dictionary<SemanticAddress, SemanticAddress> migrations)
    {
        var owner = reference.Entry;
        while (owner.Address is null && owner.Parent is { } parent)
        {
            owner = index.Find(parent)!;
        }

        // An application/module/feature can have several physical fragments. Their child indices are
        // not logical occurrence keys; keep the document and physical path for those broad scopes.
        if (owner.Address is null || owner.Address.Kind is SemanticKind.Application or SemanticKind.Module or SemanticKind.Feature)
        {
            return new(null, reference.Entry.Handle.Document, reference.Path);
        }

        return new(migrations.GetValueOrDefault(owner.Address) ?? owner.Address, default, reference.Path[owner.Handle.Path.Length..]);
    }

    sealed record ReferenceOccurrence(SemanticAddress? Owner, DocumentId Document, string Member);
}
