// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceRefactoring
{
    static readonly SemanticKind[] _movableDeclarations =
    [
        SemanticKind.Command, SemanticKind.EventContract, SemanticKind.ReadModel, SemanticKind.Projection,
        SemanticKind.Query, SemanticKind.Reaction, SemanticKind.Capture, SemanticKind.Specification
    ];

    static SemanticAddress RehomedAddress(SemanticAddress address, SemanticAddress source, SemanticAddress destination) =>
        SemanticAddress.FromCanonical(address.Kind, [.. destination.Parts, .. address.Parts.Skip(source.Parts.Length)]);

    WorkspaceAuthoringResult MoveDeclaration(WorkspaceMoveRequest request, WorkspaceSyntaxIndex index)
    {
        if (request.Target.Kind == SemanticKind.Specification) throw new InvalidWorkspaceAuthoring("A specification moves only with the declaration it covers; move that declaration instead.");
        if (request.NewParent?.Kind != SemanticKind.Slice) throw new InvalidWorkspaceAuthoring("A command, event, read model, projection, query, reaction or capture destination must be a slice.");
        var target = SingleSliceMember(index, request.Target, request.TargetHandle, "target");
        var source = Ancestors(target, index).First(entry => entry.Node is SliceSyntax);
        if (request.NewParent.Equals(source.Address)) throw new InvalidWorkspaceAuthoring("The requested parent is already the target's parent (no-op).");
        var destinations = index.Entries.Where(entry => request.NewParent.Equals(entry.Address)).ToArray();
        if (destinations.Length == 0) throw new InvalidWorkspaceAuthoring("Both logical addresses must exist in the current workspace.");
        if (request.NewParentHandle is not null && index.Find(request.NewParentHandle)?.Address?.Equals(request.NewParent) != true) throw new InvalidWorkspaceAuthoring($"Occurrence handle does not resolve to logical address '{MoveAddress(request.NewParent)}'.");
        var destination = destinations.FirstOrDefault(entry => entry.Handle == request.NewParentHandle)
            ?? destinations.FirstOrDefault(entry => entry.Handle.Document == target.Handle.Document)
            ?? (destinations.Length == 1 ? destinations[0] : throw new InvalidWorkspaceAuthoring($"Destination slice '{MoveAddress(request.NewParent)}' has fragments in several documents; name one with newParentHandle."));

        var moved = new List<WorkspaceSyntaxEntry> { target };
        foreach (var carried in request.CarriedSpecifications.Distinct())
        {
            if (carried.Kind != SemanticKind.Specification) throw new InvalidWorkspaceAuthoring($"Carried '{MoveAddress(carried)}' is not a specification.");
            var specification = SingleSliceMember(index, carried, null, "carried specification");
            if (!Ancestors(specification, index).Any(ancestor => ancestor.Node is SliceSyntax && source.Address!.Equals(ancestor.Address))) throw new InvalidWorkspaceAuthoring($"Carried specification '{MoveAddress(carried)}' is not declared in source slice '{MoveAddress(source.Address)}'.");
            moved.Add(specification);
        }

        var handles = moved.Select(entry => entry.Handle).ToHashSet();
        var subtree = index.Entries.Where(entry => handles.Contains(entry.Handle) || Ancestors(entry, index).Any(ancestor => handles.Contains(ancestor.Handle))).ToArray();
        var roots = moved.Select(entry => entry.Address!).ToArray();
        var migrations = subtree.Where(entry => entry.Address is not null).Select(entry => entry.Address!)
            .Concat(workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Address).Where(address => roots.Any(root => Within(address, root))))
            .Concat(workspace.IdentityCatalog.EventContracts.Select(assignment => assignment.Address).Where(address => roots.Any(root => Within(address, root)) || subtree.Any(entry => address.Equals(entry.Address))))
            .Where(address => Within(address, source.Address!)).Distinct()
            .ToDictionary(address => address, address => RehomedAddress(address, source.Address!, request.NewParent));
        foreach (var current in migrations.Values)
        {
            var collision = index.Entries.FirstOrDefault(entry => current.Equals(entry.Address));
            if (collision is not null) throw new InvalidWorkspaceAuthoring($"Destination collision with '{MoveAddress(collision.Address)}' at '{Position(workspace.Documents, collision)}'.");
        }

        var bindings = new WorkspaceReferenceBindings(index, includeInteractions: true);
        bindings.RequireNoCollisions();
        var documentRoots = index.Entries.Where(entry => entry.Parent is null).ToDictionary(entry => entry.Handle.Document, entry => WorkspaceSyntaxMutation.Json(entry.Node));
        var nodes = index.Entries.ToDictionary(entry => entry.Handle, entry => WorkspaceSyntaxMutation.At(documentRoots[entry.Handle.Document], entry.Handle.Path));
        var touched = new HashSet<DocumentId>();
        var repairs = ImmutableArray.CreateBuilder<WorkspaceMoveReferenceRepair>();
        foreach (var binding in bindings.Bindings.Where(binding => binding.Target?.Entry?.Address is { } address && migrations.ContainsKey(address)))
        {
            var reference = binding.Reference;
            if (!reference.Text.Contains('.')) continue;
            var replacement = string.Join('.', migrations[binding.Target!.Entry!.Address!].Parts.Skip(1).Select(part => part.Key));
            if (replacement == reference.Text) continue;
            var owner = nodes[reference.Entry.Handle];
            if (reference.Index is { } position) owner[reference.Member]![position] = replacement;
            else owner[reference.Member] = replacement;
            repairs.Add(new(reference.Entry.Handle, reference.Member, reference.Text, replacement));
            touched.Add(reference.Entry.Handle.Document);
        }

        var parent = nodes[destination.Handle];
        foreach (var entry in moved)
        {
            var node = nodes[entry.Handle];
            Detach(node);
            if (parent[entry.Member!] is not JsonArray members) parent[entry.Member!] = members = new JsonArray();
            members.Add(node);
            touched.Add(entry.Handle.Document);
        }
        touched.Add(destination.Handle.Document);

        var rehomes = migrations.Select(migration => Rehome(migration.Key, migration.Value, declaration: true)).ToArray();
        return CompleteMove(new(request, index, bindings, documentRoots, nodes, touched, repairs, migrations, [.. moved.Select(entry => entry.Handle)], rehomes, DeclarationMove: true)
        {
            Carried = [.. moved.Skip(1).Select(entry => migrations[entry.Address!])]
        });
    }

    WorkspaceSyntaxEntry SingleSliceMember(WorkspaceSyntaxIndex index, SemanticAddress address, WorkspaceNodeHandle? handle, string role)
    {
        var fragments = index.Entries.Where(entry => address.Equals(entry.Address)).ToArray();
        if (fragments.Length == 0) throw new InvalidWorkspaceAuthoring($"The {role} '{MoveAddress(address)}' does not exist in the current workspace.");
        if (fragments.Length > 1) throw new InvalidWorkspaceAuthoring($"The {role} '{MoveAddress(address)}' is declared in more than one fragment; a declaration move needs exactly one.");
        var fragment = fragments[0];
        if (handle is not null && handle != fragment.Handle) throw new InvalidWorkspaceAuthoring($"Occurrence handle does not resolve to logical address '{MoveAddress(address)}'.");
        var parent = fragment.Parent is { } owner ? index.Find(owner) : null;
        if (parent?.Node is not SliceSyntax)
        {
            var declarer = Ancestors(fragment, index).FirstOrDefault(entry => entry.Address is not null);
            throw new InvalidWorkspaceAuthoring($"'{MoveAddress(address)}' is declared inline in '{MoveAddress(declarer?.Address)}' at '{Position(workspace.Documents, fragment)}'; move '{MoveAddress(declarer?.Address)}' or extract the declaration first.");
        }
        return fragment;
    }

    static readonly string[] _sliceDeclarationMembers = ["events", "commands", "readModels", "projections", "queries", "specifications", "reactions", "captures"];

    static void SliceDeclarations(JsonObject slice, Dictionary<string, JsonNode> contents)
    {
        foreach (var member in _sliceDeclarationMembers)
        {
            if (slice[member] is JsonArray items)
            {
                foreach (var item in items) contents[member + ":" + item!["id"]!.GetValue<string>()] = item.DeepClone();
            }
            slice.Remove(member);
        }
    }

    static void RequireSliceOwnedMembers(Dictionary<string, JsonNode> original, Dictionary<string, JsonNode> candidate)
    {
        foreach (var (id, before) in original)
        {
            if (before["kind"] is null || !candidate.TryGetValue(id, out var after)) continue;
            var changed = new[] { "reducers", "constraints" }
                .Where(member => !JsonNode.DeepEquals(before[member], after[member]))
                .SelectMany(member => Names(before[member]).Concat(Names(after[member])).Distinct(StringComparer.Ordinal).Select(name => $"{member[..^1]} '{name}'"))
                .ToArray();
            if (changed.Length > 0) throw new InvalidWorkspaceAuthoring($"The move changes what slice '{before["name"]?.GetValue<string>()}' owns: {string.Join(", ", changed)}. Reducers and constraints stay with their slice; move them by hand first.");
        }
    }

    static IEnumerable<string> Names(JsonNode? members) => members is JsonArray items ? items.Select(item => item?["name"]?.GetValue<string>() ?? "?") : [];

    sealed record MoveRehome(string Prefix, string Replacement, SemanticAddress? Declaration);

    sealed record MovePlan(
        WorkspaceMoveRequest Request,
        WorkspaceSyntaxIndex Index,
        WorkspaceReferenceBindings Bindings,
        Dictionary<DocumentId, JsonNode> Roots,
        Dictionary<WorkspaceNodeHandle, JsonNode> Nodes,
        HashSet<DocumentId> Touched,
        ImmutableArray<WorkspaceMoveReferenceRepair>.Builder Repairs,
        Dictionary<SemanticAddress, SemanticAddress> Migrations,
        ImmutableArray<WorkspaceNodeHandle> Moved,
        IReadOnlyList<MoveRehome> Rehomes,
        bool DeclarationMove)
    {
        public ImmutableArray<SemanticAddress> Carried { get; init; } = [];
    }
}
