// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
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
        var routeSelections = RouteCandidateSelections(previousIndex, currentIndex, request);
        var promotions = PropertyCandidatePromotions(previousIndex, currentIndex, request);
        var previous = previousBindings.GroupBy(binding => promotions.GetValueOrDefault(binding.Reference.Entry.Handle) ?? Occurrence(binding.Reference, previousIndex, migrations))
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
                if (!routeSelections.Introduced.Contains(binding.Reference.Key)) introduced.Add(binding);
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

        var disappeared = previousBindings.Where(binding => !matched.Contains(binding.Reference.Key) && !routeSelections.Removed.Contains(binding.Reference.Key)).ToArray();
        if ((introduced.Count > 0 && disappeared.Length > 0) || (routeSelections.Removed.Count > 0 && disappeared.Length > 0))
        {
            throw new InvalidWorkspaceAuthoring("Reference occurrence correspondence cannot be proven for simultaneous removal/reordering and insertion. Split explicit removals and additions, or preserve the existing occurrence owners with semantic migrations.");
        }
    }

    // A command replacement may explicitly select one retained property candidate. Prove the
    // exact structural relocation, without weakening correspondence for arbitrary removals/additions
    // or changing source/stream identities. All existing binding/debt checks still run afterward.
    static Dictionary<WorkspaceNodeHandle, ReferenceOccurrence> PropertyCandidatePromotions(
        WorkspaceSyntaxIndex before, WorkspaceSyntaxIndex after, WorkspaceAuthoringRequest request)
    {
        var result = new Dictionary<WorkspaceNodeHandle, ReferenceOccurrence>();
        var comparer = (IEqualityComparer<SyntaxNode>)ReferenceEqualityComparer.Instance;
        var originalNodes = before.Entries.ToLookup(entry => entry.Node, comparer);
        var currentNodes = after.Entries.ToLookup(entry => entry.Node, comparer);
        var commandOwners = after.Entries.Where(entry => entry.Node is CommandSyntax && entry.Address is not null)
            .ToLookup(entry => (entry.Handle.Document, entry.Address));
        foreach (var operation in request.Operations.OfType<ReplaceWorkspaceNode>())
        {
            var entry = before.Find(operation.Target);
            if (entry?.Node is not CommandSyntax command || entry.Address is null || operation.Node is not CommandSyntax replacement) continue;
            foreach (var candidate in command.StreamCandidates.Where(candidate => candidate.PropertyCandidate is not null))
            {
                var property = candidate.PropertyCandidate!;
                var intended = command with
                {
                    Properties = [.. command.Properties, property],
                    StreamCandidates = [.. command.StreamCandidates.Where(value => !ReferenceEquals(value, candidate))]
                };
                if (!SyntaxJson.StructurallyEqual(intended, replacement)) continue;
                var owners = commandOwners[(entry.Handle.Document, entry.Address)].Take(2).ToArray();
                if (owners is not [var owner] || owner.Node is not CommandSyntax current) continue;
                var properties = current.Properties.Where(value => SyntaxJson.StructurallyEqual(value, property)).Take(2).ToArray();
                if (properties is not [var selected]) continue;
                var originalTypes = originalNodes[property.Type].Take(2).ToArray();
                var selectedTypes = currentNodes[selected.Type].Take(2).ToArray();
                if (originalTypes is [var original] && selectedTypes is [var target])
                {
                    var reference = new WorkspaceReferenceMember(target, "name", null, selected.Type.Name, WorkspaceReferenceDomain.Type);
                    result[original.Handle] = Occurrence(reference, after, []);
                }
            }
        }

        return result;
    }

    // Only a command replacement selecting an exact retained route can account for the type
    // candidate disappearing and the two route references appearing. Do not infer a route from
    // a property, waive debt checks, or excuse any other removed reference in the same proposal.
    static (HashSet<string> Removed, HashSet<string> Introduced) RouteCandidateSelections(
        WorkspaceSyntaxIndex before, WorkspaceSyntaxIndex after, WorkspaceAuthoringRequest request)
    {
        var removed = new HashSet<string>(StringComparer.Ordinal);
        var introduced = new HashSet<string>(StringComparer.Ordinal);
        WorkspacePhysicalReadView? previousFacts = null;
        WorkspacePhysicalReadView? currentFacts = null;
        foreach (var operation in request.Operations.OfType<ReplaceWorkspaceNode>())
        {
            var entry = before.Find(operation.Target);
            if (entry?.Node is not CommandSyntax { Stream: null } command || entry.Address is null ||
                operation.Node is not CommandSyntax { Stream: { PropertyCandidate: null } route } replacement)
            {
                continue;
            }
            var candidates = command.StreamCandidates.Where(candidate => candidate.PropertyCandidate is not null &&
                candidate.EventSource == route.EventSource && candidate.Stream == route.Stream && RetainedRouteSpan(before.Workspace, entry, candidate)).Take(2).ToArray();
            if (candidates is not [var selected]) continue;
            var intended = command with
            {
                Stream = selected with { PropertyCandidate = null, StreamId = route.StreamId },
                StreamCandidates = [.. command.StreamCandidates.Where(candidate => !ReferenceEquals(candidate, selected))]
            };
            if (!SyntaxJson.StructurallyEqual(intended, replacement)) continue;
            var owners = after.Entries.Where(value => value.Handle.Document == entry.Handle.Document && entry.Address.Equals(value.Address) && value.Node is CommandSyntax).Take(2).ToArray();
            if (owners is not [var owner] || owner.Node is not CommandSyntax { Stream: { } current } ||
                !SyntaxJson.StructurallyEqual(current, route))
            {
                continue;
            }
            previousFacts ??= WorkspacePhysicalReadView.Create(before.Workspace);
            currentFacts ??= WorkspacePhysicalReadView.Create(after.Workspace);
            if (!previousFacts.IsComplete || !currentFacts.IsComplete || !SamePhysicalRoute(previousFacts, currentFacts, route))
            {
                throw new InvalidWorkspaceAuthoring("Cannot prove explicit stream candidate selection: the source and stream must have unchanged, unique physical declarations in resolved, complete source.");
            }
            var candidateTypes = before.Entries.Where(value => ReferenceEquals(value.Node, selected.PropertyCandidate!.Type)).Take(2).ToArray();
            var routeEntries = after.Entries.Where(value => ReferenceEquals(value.Node, current)).Take(2).ToArray();
            if (candidateTypes is not [var candidateType] || routeEntries is not [var routeEntry]) continue;
            removed.Add(new WorkspaceReferenceMember(candidateType, "name", null, selected.PropertyCandidate!.Type.Name, WorkspaceReferenceDomain.Type).Key);
            introduced.Add(new WorkspaceReferenceMember(routeEntry, "eventSource", null, route.EventSource, WorkspaceReferenceDomain.EventSource).Key);
            introduced.Add(new WorkspaceReferenceMember(routeEntry, "stream", null, route.Stream, WorkspaceReferenceDomain.EventStream, route.EventSource).Key);
        }

        return (removed, introduced);
    }

    static bool RetainedRouteSpan(ScreenplayWorkspace workspace, WorkspaceSyntaxEntry command, CommandStreamSyntax candidate)
    {
        var name = $"{candidate.EventSource}.{candidate.Stream}";
        var type = candidate.PropertyCandidate!.Type;
        if (type.Name != name || candidate.ReferenceLocation != type.Location || candidate.ReferenceLength != name.Length || candidate.ReferenceLocation is not { } location) return false;
        var document = workspace.Documents.Single(document => document.Id == command.Handle.Document);
        var lines = SourceLineSplitter.Split(document.Text, path: document.Path.Value);
        var line = lines.SingleOrDefault(line => line.Number == location.Line);

        return line is not null && location.Column - line.Indent - 1 is var offset && offset >= 0 &&
            offset + name.Length <= line.Content.Length && line.Content.AsSpan(offset, name.Length).SequenceEqual(name);
    }

    static bool SamePhysicalRoute(WorkspacePhysicalReadView before, WorkspacePhysicalReadView after, CommandStreamSyntax route)
    {
        static WorkspaceSyntaxEntry[] Owners(WorkspacePhysicalReadView facts, string source) =>
            [.. facts.Entries.Where(entry => entry.Node is EventSourceSyntax declaration && declaration.Name == source).Take(2)];
        var previous = Owners(before, route.EventSource);
        var current = Owners(after, route.EventSource);
        if (previous is not [var original] || current is not [var retained] ||
            original.Handle.Document != retained.Handle.Document || original.Handle.Path != retained.Handle.Path ||
            !SyntaxJson.StructurallyEqual(original.Node, retained.Node))
        {
            return false;
        }
        var streams = before.Entries.Where(entry => entry.Parent == original.Handle && entry.Node is EventStreamSyntax stream && stream.Name == route.Stream).Take(2).ToArray();

        return streams.Length == 1;
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
