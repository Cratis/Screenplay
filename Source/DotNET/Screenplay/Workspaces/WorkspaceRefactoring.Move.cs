// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceRefactoring
{
    static readonly string[] _moveWrapperMembers = ["kind", "name", "isPlacement"];

    internal WorkspaceAuthoringResult Move(WorkspaceMoveRequest request)
    {
        if (request is null) return Failure(WorkspaceConflictKind.InvalidOperation, "A move request is required.");
        if (request.ExpectedRevision != workspace.Revision) return Failure(WorkspaceConflictKind.StaleWorkspaceRevision, "The move has a stale workspace revision.");
        if (request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision) return Failure(WorkspaceConflictKind.StaleCatalogRevision, "The move has a stale catalog revision.");
        try
        {
            return MoveCore(request);
        }
        catch (Exception exception) when (exception is InvalidWorkspaceAuthoring or InvalidSemanticContract)
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, $"Move '{MoveAddress(request.Target)}' to '{MoveAddress(request.NewParent)}' refused: {exception.Message}");
        }
    }

    static string MoveAddress(SemanticAddress? address) => address is null ? "unassigned" : $"{address.Kind}:{string.Join('.', address.Parts.Skip(1).Select(part => part.Key))}";

    static bool Within(SemanticAddress address, SemanticAddress prefix) => address.Parts.Length >= prefix.Parts.Length && address.Parts.Take(prefix.Parts.Length).SequenceEqual(prefix.Parts);

    static SemanticAddress MovedAddress(SemanticAddress address, WorkspaceMoveRequest request) => Within(address, request.Target)
        ? SemanticAddress.FromCanonical(address.Kind, [.. request.NewParent.Parts, .. address.Parts.Skip(request.Target.Parts.Length - 1)])
        : address;

    static void Detach(JsonNode node)
    {
        if (node.Parent is not JsonArray array) throw new InvalidWorkspaceAuthoring("The moved fragment must occupy a typed collection.");
        array.Remove(node);
    }

    static Dictionary<JsonNode, (DocumentId Document, string Path)> NodeLocations(Dictionary<DocumentId, JsonNode> roots)
    {
        var locations = new Dictionary<JsonNode, (DocumentId Document, string Path)>(ReferenceEqualityComparer.Instance);
        void Visit(JsonNode node, DocumentId document, string path)
        {
            locations.Add(node, (document, path));
            if (node is JsonObject value)
            {
                foreach (var pair in value.Where(pair => pair.Value is not null)) Visit(pair.Value!, document, path + "/" + pair.Key);
            }
            else if (node is JsonArray children)
            {
                for (var index = 0; index < children.Count; index++)
                {
                    if (children[index] is { } child) Visit(child, document, path + "/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
        }
        foreach (var (document, root) in roots) Visit(root, document, string.Empty);
        return locations;
    }

    static JsonNode Destination(JsonNode root, SemanticAddress address, IReadOnlyList<string> placementScope)
    {
        var current = root;
        var scope = new List<string>();
        foreach (var part in address.Parts.Skip(1))
        {
            scope.Add(part.Key);
            var placement = scope.Count <= placementScope.Count && scope.SequenceEqual(placementScope.Take(scope.Count));
            var member = part.Kind == SemanticAddressPartKind.Module ? "modules" : "features";
            var children = (JsonArray)current[member]!;
            var next = children.FirstOrDefault(child => child!["name"]!.GetValue<string>() == part.Key);
            if (next is null)
            {
                SyntaxNode syntax = part.Kind == SemanticAddressPartKind.Module
                    ? new ModuleSyntax(part.Key, [], [], SourceLocation.Start) { IsPlacement = placement }
                    : new FeatureSyntax(part.Key, [], [], SourceLocation.Start) { IsPlacement = placement };
                next = WorkspaceSyntaxMutation.Json(syntax);
                children.Add(next);
            }
            current = next;
        }
        return current;
    }

    static bool EmptyWrapper(JsonNode node) => node is JsonObject value && value.All(pair => _moveWrapperMembers.Contains(pair.Key, StringComparer.Ordinal) || pair.Value is null or JsonArray { Count: 0 });

    static Dictionary<string, JsonNode> ExecutableContents(ScreenplayWorkspace source)
    {
        var root = JsonNode.Parse(SemanticModelCanonicalJson.Serialize(source.Compilation.Value!.Model))!;
        var contents = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        void Container(JsonNode node)
        {
            foreach (var member in new[] { "modules", "features", "slices" })
            {
                if (node[member] is not JsonArray children) continue;
                foreach (var child in children.ToArray()) Container(child!);
                ((JsonObject)node).Remove(member);
            }
            contents.Add(node["id"]!.GetValue<string>(), node.DeepClone());
        }
        Container(root["application"]!);
        return contents;
    }

    static void RequireExecutableMoveContinuity(ScreenplayWorkspace before, ScreenplayWorkspace after)
    {
        if (!before.Compilation.Success)
        {
            if (after.Compilation.Success) throw new InvalidWorkspaceAuthoring("The move changes executable admission; the original model has no executable model to compare.");
            return;
        }
        if (!after.Compilation.Success) throw new InvalidWorkspaceAuthoring("The move made the executable model unavailable: " + string.Join(" | ", after.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message)));
        var original = ExecutableContents(before);
        var candidate = ExecutableContents(after);
        var differences = original.Keys.Union(candidate.Keys).Where(id => !original.TryGetValue(id, out var old) || !candidate.TryGetValue(id, out var current) || !JsonNode.DeepEquals(old, current)).ToArray();
        if (differences.Length > 0) throw new InvalidWorkspaceAuthoring("Executable model differs beyond ownership addresses (including inherited authorize): " + string.Join(", ", differences));
    }

    static ApplicationSyntax Merged(WorkspaceSyntaxIndex index) => PlayFolderMerge.Merge([.. index.Entries.Where(entry => entry.Parent is null).Select(entry => new CompilationResult<ApplicationSyntax>((ApplicationSyntax)entry.Node, []))]).Value ?? throw new InvalidWorkspaceAuthoring("Logical fragments cannot be merged for continuity validation.");

    static Dictionary<string, string> InheritedBehavior(WorkspaceSyntaxIndex index, WorkspaceMoveRequest? migration = null)
    {
        var bindings = new WorkspaceReferenceBindings(index, includeInteractions: true);
        var targets = new Dictionary<SyntaxNode, List<string>>(ReferenceEqualityComparer.Instance);
        var attachments = new Dictionary<SyntaxNode, JsonNode>(ReferenceEqualityComparer.Instance);
        foreach (var binding in bindings.Bindings.Where(binding => binding.Reference.Entry.Node is InteractionActionSyntax or BehaviorArgumentSyntax))
        {
            var ownerEntry = WorkspaceReferenceMembers.Parents(binding.Reference.Entry, index).First(entry => entry.Node is BehaviorSyntax or UsesBehaviorSyntax);
            var owner = ownerEntry.Node;
            if (!targets.TryGetValue(owner, out var values)) targets[owner] = values = [];
            var target = binding.Target;
            var path = target is null ? binding.Reference.Text : string.Join('.', target.Scope.Segments.Append(target.Name));
            if (migration is not null && target?.Entry is not null)
            {
                var prefix = string.Join('.', migration.Target.Parts.Skip(1).Select(part => part.Key));
                var replacement = string.Join('.', MovedAddress(migration.Target, migration).Parts.Skip(1).Select(part => part.Key));
                if (path.StartsWith(prefix + ".", StringComparison.Ordinal))
                {
                    path = replacement + path[prefix.Length..];
                    var reference = binding.Reference;
                    if (reference.Text.Contains('.'))
                    {
                        if (!attachments.TryGetValue(owner, out var attachment)) attachments[owner] = attachment = WorkspaceSyntaxMutation.Json(owner);
                        var referenceNode = WorkspaceSyntaxMutation.At(attachment, reference.Entry.Handle.Path[ownerEntry.Handle.Path.Length..]);
                        if (reference.Index is { } position) referenceNode[reference.Member]![position] = path;
                        else referenceNode[reference.Member] = path;
                    }
                }
            }
            values.Add($"{binding.Reference.Domain}:{binding.Outcome}:{path}");
        }
        var named = index.Entries.Select(entry => entry.Node).OfType<BehaviorSyntax>().Where(behavior => behavior.Name is not null)
            .GroupBy(behavior => behavior.Name!).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        string Fingerprint(SyntaxNode node)
        {
            var resolved = targets.GetValueOrDefault(node) ?? [];
            if (node is UsesBehaviorSyntax uses)
            {
                if (!named.TryGetValue(uses.Behavior, out var declarations) || declarations.Length != 1)
                    throw new InvalidWorkspaceAuthoring($"Inherited interaction behavior '{uses.Behavior}' cannot be resolved uniquely for move continuity.");
                resolved = [.. resolved, .. targets.GetValueOrDefault(declarations[0]) ?? []];
            }
            var attachment = (attachments.GetValueOrDefault(node) ?? WorkspaceSyntaxMutation.Json(node)).ToJsonString(new() { MaxDepth = 256 });
            return attachment + "|targets:" + string.Join(',', resolved);
        }
        var merged = WorkspaceSyntaxIndex.ForSyntax(Merged(index), index.Workspace.IdentityCatalog);
        var byHandle = merged.ToDictionary(entry => entry.Handle);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in merged.Where(entry => entry.Node is ScreenSyntax or CommandSyntax or QuerySyntax))
        {
            var ancestors = new List<SyntaxNode>();
            var parent = entry.Parent;
            while (parent is not null)
            {
                var ancestor = byHandle[parent];
                ancestors.Add(ancestor.Node);
                parent = ancestor.Parent;
            }
            ancestors.Reverse();
            string[] members = entry.Node is ScreenSyntax ? ["Behaviors", "UsedBehaviors"] : ["Authorize"];
            var values = ancestors.SelectMany(node => members.Select(member => node.GetType().GetProperty(member)?.GetValue(node)))
                .SelectMany(BehaviorNodes)
                .Select(Fingerprint);
            var scope = ancestors.Where(node => node is ModuleSyntax or FeatureSyntax or SliceSyntax).Select(WorkspaceReferenceBindings.Name);
            result[$"{entry.Kind}:{string.Join('.', scope.Append(WorkspaceReferenceBindings.Name(entry.Node)))}"] = string.Join('|', values);
        }
        return result;
    }

    static IEnumerable<SyntaxNode> BehaviorNodes(object? value) => value switch
    {
        IEnumerable<SyntaxNode> nodes => nodes,
        SyntaxNode node => [node],
        _ => []
    };

    static void RequireInheritedBehavior(WorkspaceSyntaxIndex before, WorkspaceSyntaxIndex after, WorkspaceMoveRequest request)
    {
        var previous = InheritedBehavior(before, request);
        var current = InheritedBehavior(after);
        var prefix = string.Join('.', request.Target.Parts.Skip(1).Select(part => part.Key));
        var replacement = string.Join('.', MovedAddress(request.Target, request).Parts.Skip(1).Select(part => part.Key));
        foreach (var (key, value) in previous)
        {
            var separator = key.IndexOf(':');
            var path = key[(separator + 1)..];
            var migrated = path.StartsWith(prefix + ".", StringComparison.Ordinal) ? key[..(separator + 1)] + replacement + path[prefix.Length..] : key;
            if (current.GetValueOrDefault(migrated) != value)
            {
                throw new InvalidWorkspaceAuthoring($"Inherited authorize or screen on/uses interaction bindings differ at '{path}' -> '{migrated}': previous [{value}], current [{current.GetValueOrDefault(migrated)}].");
            }
        }
    }

    WorkspaceAuthoringResult MoveCore(WorkspaceMoveRequest request)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (index.Entries.Count(entry => entry.Parent is null) != workspace.Documents.Length) throw new InvalidWorkspaceAuthoring("Every document must parse and have resolved placement before moving.");
        if (request.Target is null || request.NewParent is null || request.Target.Kind is not (SemanticKind.Slice or SemanticKind.Feature) ||
            (request.Target.Kind == SemanticKind.Slice ? request.NewParent.Kind != SemanticKind.Feature : request.NewParent.Kind is not (SemanticKind.Module or SemanticKind.Feature)))
        {
            throw new InvalidWorkspaceAuthoring("A slice destination must be a feature; a feature destination must be a module or feature. Declaration moves between slices are deferred.");
        }
        var fragments = index.Entries.Where(entry => request.Target.Equals(entry.Address)).ToArray();
        var destinations = index.Entries.Where(entry => request.NewParent.Equals(entry.Address)).ToArray();
        if (fragments.Length == 0 || destinations.Length == 0) throw new InvalidWorkspaceAuthoring("Both logical addresses must exist in the current workspace.");
        foreach (var (handle, address) in new[] { (request.TargetHandle, request.Target), (request.NewParentHandle, request.NewParent) })
        {
            if (handle is not null && index.Find(handle)?.Address?.Equals(address) != true) throw new InvalidWorkspaceAuthoring($"Occurrence handle does not resolve to logical address '{MoveAddress(address)}'.");
        }
        if (Within(request.NewParent, request.Target)) throw new InvalidWorkspaceAuthoring("A subtree cannot move into itself or a descendant.");
        var currentAddress = MovedAddress(request.Target, request);
        if (currentAddress.Equals(request.Target)) throw new InvalidWorkspaceAuthoring("The requested parent is already the target's parent (no-op).");
        var collision = index.Entries.FirstOrDefault(entry => currentAddress.Equals(entry.Address));
        if (collision is not null) throw new InvalidWorkspaceAuthoring($"Destination collision with '{MoveAddress(collision.Address)}' at '{Position(workspace.Documents, collision)}'.");

        var bindings = new WorkspaceReferenceBindings(index, includeInteractions: true);
        bindings.RequireNoCollisions();
        var roots = index.Entries.Where(entry => entry.Parent is null).ToDictionary(entry => entry.Handle.Document, entry => WorkspaceSyntaxMutation.Json(entry.Node));
        var nodes = index.Entries.ToDictionary(entry => entry.Handle, entry => WorkspaceSyntaxMutation.At(roots[entry.Handle.Document], entry.Handle.Path));
        var touched = new HashSet<DocumentId>();
        var repairs = ImmutableArray.CreateBuilder<WorkspaceMoveReferenceRepair>();
        var migrations = index.Entries.Where(entry => entry.Address is not null).Select(entry => entry.Address!)
            .Concat(workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Address))
            .Concat(workspace.IdentityCatalog.EventContracts.Select(assignment => assignment.Address))
            .Where(address => Within(address, request.Target)).Distinct()
            .ToDictionary(address => address, address => MovedAddress(address, request));
        foreach (var binding in bindings.Bindings.Where(binding => binding.Target?.Entry is not null))
        {
            var target = binding.Target!.Entry!;
            var scope = WorkspaceReferenceBindings.Scope(target, index).Segments;
            var names = scope.Append(binding.Target.Name).ToArray();
            var prefix = request.Target.Parts.Skip(1).Select(part => part.Key).ToArray();
            if (!names.Take(prefix.Length).SequenceEqual(prefix)) continue;
            if (!binding.Reference.Text.Contains('.') && binding.Reference.Domain != WorkspaceReferenceDomain.Container) continue;
            var replacement = string.Join('.', request.NewParent.Parts.Skip(1).Select(part => part.Key).Concat(names.Skip(prefix.Length - 1)));
            if (replacement == binding.Reference.Text) continue;
            var reference = binding.Reference;
            var owner = nodes[reference.Entry.Handle];
            if (reference.Index is { } position) owner[reference.Member]![position] = replacement;
            else owner[reference.Member] = replacement;
            repairs.Add(new(reference.Entry.Handle, reference.Member, reference.Text, replacement));
            touched.Add(reference.Entry.Handle.Document);
        }

        var placements = ReparentImports(index, request, fragments, destinations, roots, nodes, touched);
        foreach (var fragment in fragments)
        {
            var node = nodes[fragment.Handle];
            var document = workspace.Documents.Single(document => document.Id == fragment.Handle.Document);
            var placement = placements.GetValueOrDefault(document.Id) ?? index.Placement(document).Scope;
            if (!request.NewParent.Parts.Skip(1).Select(part => part.Key).Take(placement.Count).SequenceEqual(placement))
                throw new InvalidWorkspaceAuthoring($"Document '{document.Path}' remains placed at '{string.Join('.', placement)}', outside destination '{MoveAddress(request.NewParent)}'.");
            Detach(node);
            var parentFragment = destinations.FirstOrDefault(destination => destination.Handle.Document == fragment.Handle.Document);
            var destination = parentFragment is null ? Destination(roots[fragment.Handle.Document], request.NewParent, placement) : nodes[parentFragment.Handle];
            ((JsonArray)destination[request.Target.Kind == SemanticKind.Slice ? "slices" : "features"]!).Add(node);
            touched.Add(fragment.Handle.Document);
        }

        // Remove redundant empty physical wrappers, never the last occurrence of an old logical parent.
        foreach (var ancestor in fragments.SelectMany(fragment => Ancestors(fragment, index)).Where(entry => entry.Node is ModuleSyntax or FeatureSyntax).DistinctBy(entry => entry.Handle))
        {
            var node = nodes[ancestor.Handle];
            if (node.Parent is JsonArray && EmptyWrapper(node) && index.Entries.Count(entry => ancestor.Address!.Equals(entry.Address) && nodes[entry.Handle].Parent is not null) > 1) Detach(node);
        }

        var operations = touched.Select(document => (WorkspaceOperation)new ReplaceWorkspaceSyntaxDocument(document, WorkspaceSyntaxMutation.Syntax(roots[document]))).ToImmutableArray();
        var semanticRenames = workspace.IdentityCatalog.Semantics.Where(assignment => migrations.ContainsKey(assignment.Address)).Select(assignment => new SemanticIdentityRename(assignment.Address, migrations[assignment.Address])).ToImmutableArray();
        var eventRenames = workspace.IdentityCatalog.EventContracts.Where(assignment => migrations.ContainsKey(assignment.Address)).Select(assignment => new EventContractIdentityRename(assignment.Address, migrations[assignment.Address])).ToImmutableArray();
        var locations = NodeLocations(roots);
        var lineage = new Dictionary<(DocumentId Document, string Path), (DocumentId Document, string Path)>();
        foreach (var entry in index.Entries.Where(entry => touched.Contains(entry.Handle.Document)))
        {
            if (locations.TryGetValue(nodes[entry.Handle], out var location)) lineage[(entry.Handle.Document, entry.Handle.Path)] = location;
        }
        var result = new WorkspaceAuthoringTransaction(workspace, migrations, movedOccurrences: lineage).Propose(new()
        {
            ExpectedRevision = request.ExpectedRevision,
            ExpectedCatalogRevision = request.ExpectedCatalogRevision,
            Formatting = request.Formatting,
            Validation = request.Validation,
            Documents = operations,
            SemanticRenames = semanticRenames,
            EventRenames = eventRenames
        });
        if (!result.Accepted) return result;
        var candidateIndex = WorkspaceSyntaxIndex.Create(result.Workspace!);
        var candidateBindings = new WorkspaceReferenceBindings(candidateIndex, includeInteractions: true);
        candidateBindings.RequireNoCollisions();
        RequireMoveReferences(candidateIndex, bindings, candidateBindings, locations, nodes, migrations);
        foreach (var container in index.Entries.Where(entry => entry.Node is ModuleSyntax or FeatureSyntax).Select(entry => entry.Address!).Distinct())
        {
            var expected = migrations.GetValueOrDefault(container) ?? container;
            if (!candidateIndex.Entries.Any(entry => expected.Equals(entry.Address))) throw new InvalidWorkspaceAuthoring($"The move lost logical parent '{MoveAddress(container)}'.");
        }
        RequireInheritedBehavior(index, candidateIndex, request);
        RequireExecutableMoveContinuity(workspace, result.Workspace!);
        if (workspace.IdentityCatalog.Semantics.Any(old => !result.Workspace!.IdentityCatalog.Semantics.Any(current => current.Id == old.Id)) ||
            workspace.IdentityCatalog.EventContracts.Any(old => !result.Workspace!.IdentityCatalog.EventContracts.Any(current => current.Id == old.Id)))
        {
            throw new InvalidWorkspaceAuthoring("A move cannot retire any assigned semantic or event-contract identity.");
        }
        return result with
        {
            MoveReport = new(
                [.. semanticRenames.Select(rename => new WorkspaceMoveIdentityMigration("semantic", workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Equals(rename.PreviousAddress)).Id.ToString(), rename.PreviousAddress, rename.CurrentAddress)),
                 .. eventRenames.Select(rename => new WorkspaceMoveIdentityMigration("event", workspace.IdentityCatalog.EventContracts.Single(assignment => assignment.Address.Equals(rename.PreviousAddress)).Id.ToString(), rename.PreviousAddress, rename.CurrentAddress))],
                repairs.ToImmutable(),
                [.. fragments.Select(fragment => fragment.Handle)])
        };
    }

    Dictionary<DocumentId, IReadOnlyList<string>> ReparentImports(
        WorkspaceSyntaxIndex index,
        WorkspaceMoveRequest request,
        WorkspaceSyntaxEntry[] fragments,
        WorkspaceSyntaxEntry[] destinations,
        Dictionary<DocumentId, JsonNode> roots,
        Dictionary<WorkspaceNodeHandle, JsonNode> nodes,
        HashSet<DocumentId> touched)
    {
        var placements = new Dictionary<DocumentId, IReadOnlyList<string>>();
        foreach (var entry in index.Entries.Where(entry => entry.Node is FileImportSyntax))
        {
            if (Ancestors(entry, index).Any(ancestor => request.Target.Equals(ancestor.Address))) continue;
            var scope = WorkspaceReferenceBindings.Scope(entry, index).Segments.ToArray();
            var oldParent = request.Target.Parts.Skip(1).SkipLast(1).Select(part => part.Key).ToArray();
            if (!scope.SequenceEqual(oldParent)) continue;
            var importer = workspace.Documents.Single(document => document.Id == entry.Handle.Document);
            var import = (FileImportSyntax)entry.Node;
            var pattern = PlayGlob.Resolve(importer.Path.Value, import.Pattern);
            var matched = workspace.Documents.Where(document => document.Id != importer.Id && PlayGlob.IsMatch(pattern, document.Path.Value)).ToArray();
            var affected = matched.Where(document => index.Placement(document).Scope.SequenceEqual(oldParent) &&
                fragments.Any(fragment => fragment.Handle.Document == document.Id)).ToArray();
            if (affected.Length == 0) continue;
            if (PlayGlob.HasWildcard(import.Pattern)) throw new InvalidWorkspaceAuthoring($"Import '{import.Pattern}' at '{importer.Path}' is a glob placing moved fragments; literal-path imports are required. Matched addresses: {string.Join(", ", matched.Select(document => document.Path.Value))}.");
            if (matched.Length != 1 || index.Entries.Any(candidate => candidate.Handle.Document == matched[0].Id && candidate.Address is not null &&
                candidate.Node is not (ApplicationSyntax or ModuleSyntax { IsPlacement: true } or FeatureSyntax { IsPlacement: true }) && !Within(candidate.Address, request.Target)))
            {
                throw new InvalidWorkspaceAuthoring($"Import '{import.Pattern}' also places declarations outside '{MoveAddress(request.Target)}'.");
            }
            var authored = destinations.Where(destination => destination.Node is not (ModuleSyntax { IsPlacement: true } or FeatureSyntax { IsPlacement: true })).ToArray();
            var ownNamed = authored.Where(destination => Path.GetFileNameWithoutExtension(workspace.Documents.Single(document => document.Id == destination.Handle.Document).Path.Value) == request.NewParent.Name).ToArray();
            var receiver = authored.Length == 1 ? authored : ownNamed;
            var destination = (receiver.Length == 1 ? receiver[0] : null)
                ?? throw new InvalidWorkspaceAuthoring($"Split destination '{MoveAddress(request.NewParent)}' has no single own-named file for import '{import.Pattern}'.");
            var destinationDocument = workspace.Documents.Single(document => document.Id == destination.Handle.Document);
            var node = nodes[entry.Handle];
            Detach(node);
            node["pattern"] = Path.GetRelativePath(Path.GetDirectoryName(destinationDocument.Path.Value) is { Length: > 0 } directory ? directory : ".", matched[0].Path.Value).Replace('\\', '/');
            ((JsonArray)nodes[destination.Handle]["fileImports"]!).Add(node);
            touched.Add(entry.Handle.Document);
            touched.Add(destination.Handle.Document);
            placements[matched[0].Id] = [.. request.NewParent.Parts.Skip(1).Select(part => part.Key)];
        }
        return placements;
    }

    void RequireMoveReferences(
        WorkspaceSyntaxIndex after,
        WorkspaceReferenceBindings previous,
        WorkspaceReferenceBindings current,
        Dictionary<JsonNode, (DocumentId Document, string Path)> locations,
        Dictionary<WorkspaceNodeHandle, JsonNode> nodes,
        Dictionary<SemanticAddress, SemanticAddress> migrations)
    {
        var entries = after.Entries.ToDictionary(entry => (entry.Handle.Document, entry.Handle.Path));
        WorkspaceSyntaxEntry? Counterpart(WorkspaceSyntaxEntry original)
        {
            return locations.TryGetValue(nodes[original.Handle], out var location) ? entries.GetValueOrDefault(location) : null;
        }
        var currentByOccurrence = current.Bindings.ToDictionary(binding => (binding.Reference.Entry.Handle, binding.Reference.Member, binding.Reference.Index));
        var remaining = current.Bindings.Select(binding => binding.Reference.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var binding in previous.Bindings)
        {
            var occurrence = Counterpart(binding.Reference.Entry);
            var candidate = occurrence is null ? null : currentByOccurrence.GetValueOrDefault((occurrence.Handle, binding.Reference.Member, binding.Reference.Index));
            if (candidate is not null) remaining.Remove(candidate.Reference.Key);
            var originalTarget = binding.Target?.Entry;
            var expected = originalTarget?.Address is { } address ? migrations.GetValueOrDefault(address) ?? address : null;
            var sameTarget = binding.Target?.Key == candidate?.Target?.Key;
            if (expected is not null) sameTarget = expected.Equals(candidate?.Target?.Entry?.Address);
            else if (originalTarget is not null) sameTarget = Counterpart(originalTarget)?.Handle == candidate?.Target?.Entry?.Handle;
            if (candidate is null || candidate.Outcome != binding.Outcome || !sameTarget || (binding.Target is null && binding.Reference.Text != candidate.Reference.Text))
            {
                throw new InvalidWorkspaceAuthoring($"Reference capture, ambiguity or loss at '{binding.Reference.Key}': '{binding.Reference.Text}' ({binding.Outcome}, {MoveAddress(binding.Target?.Entry?.Address)}) -> '{candidate?.Reference.Text}' ({candidate?.Outcome}, {MoveAddress(candidate?.Target?.Entry?.Address)}). Candidates: {string.Join(", ", after.Entries.Where(entry => WorkspaceReferenceBindings.Name(entry.Node) == binding.Reference.Text.Split('.')[^1]).Select(entry => entry.Address is null ? entry.Handle.Path : MoveAddress(entry.Address)))}.");
            }
        }
        if (remaining.Count > 0) throw new InvalidWorkspaceAuthoring("Move introduced unexpected reference occurrences: " + string.Join(", ", remaining));
    }
}
