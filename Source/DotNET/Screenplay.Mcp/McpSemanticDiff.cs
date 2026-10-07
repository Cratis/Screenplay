// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// This is a structural review, not the execution/equivalence verdict from decision 0013.
static class McpSemanticDiff
{
    static readonly string[] _sections = ["declarations", "events", "members", "specifications", "dependants", "identities"];
    static readonly string[] _structuralSections = ["events", "members", "specifications"];
    static readonly string[] _ignoredMembers = ["kind", "name", "id", "description", "documentation", "file"];
    static readonly string[] _opaqueMembers = ["code", "body", "content", "file", "description", "documentation"];
    internal static object Read(IMcpProposal proposal, JsonElement arguments)
    {
        var revision = proposal.Workspace.Revision.ToString();
        var expected = McpJson.OptionalString(arguments, "expectedSourceRevision");
        if (expected is not null && expected != revision)
        {
            throw new McpFailure("StaleRevision: semantic-diff pages must identify the proposal revision.");
        }
        if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 && expected is null)
        {
            throw new McpFailure("'expectedSourceRevision' is required for continuation.", -32602);
        }

        var before = new Snapshot(proposal.Before);
        var after = new Snapshot(proposal.Workspace);
        var changes = new List<Change>();
        var changedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in before.Assignments.Keys.Union(after.Assignments.Keys).Order(StringComparer.Ordinal))
        {
            before.Assignments.TryGetValue(id, out var old);
            after.Assignments.TryGetValue(id, out var current);
            var address = current?.Address ?? old!.Address;
            var kind = address.Kind.ToString();
            var previous = old is null ? null : Address(old.Address);
            var next = current is null ? null : Address(current.Address);
            if (old is null || current is null)
            {
                var change = old is null ? "added" : "removed";
                Add(new("declarations", change, id, kind, previous, next, BeforeDocuments: before.Documents(id), AfterDocuments: after.Documents(id)));
                changes.Add(new("identities", old is null ? "assigned" : "retired", id, kind, previous, next));
                if (address.Kind == SemanticKind.Specification) changes.Add(new("specifications", change, id, kind, previous, next));
                continue;
            }

            if (!old.Address.Equals(current.Address))
            {
                var change = old.Address.Name != current.Address.Name ? "renamed" : "moved";
                Add(new("declarations", change, id, kind, previous, next, BeforeDocuments: before.Documents(id), AfterDocuments: after.Documents(id)));
                changes.Add(new("identities", "migrated", id, kind, previous, next));
            }
            var oldNodes = before.Nodes.GetValueOrDefault(id) ?? [];
            var newNodes = after.Nodes.GetValueOrDefault(id) ?? [];
            if (!before.Comparable(id) || !after.Comparable(id)) continue;
            var oldPaths = before.Documents(id);
            var newPaths = after.Documents(id);
            if (!oldPaths.SequenceEqual(newPaths) && (old.Address.Equals(current.Address) || old.Address.Name != current.Address.Name))
            {
                Add(new("declarations", "moved", id, kind, previous, next, BeforeDocuments: oldPaths, AfterDocuments: newPaths));
            }

            if (address.Kind == SemanticKind.EventContract)
            {
                Events(id, previous!, next!, [.. oldNodes.Select(entry => entry.Node).OfType<EventSyntax>()], [.. newNodes.Select(entry => entry.Node).OfType<EventSyntax>()], changes, changedIds);
            }
            if (address.Kind is not (SemanticKind.Application or SemanticKind.Module or SemanticKind.Feature or SemanticKind.Slice))
            {
                // Split hierarchy scaffolds are not compared as subtrees; descendants have their own IDs.
                var left = Members(oldNodes[0].Node);
                var right = Members(newNodes[0].Node);
                foreach (var member in left.Keys.Union(right.Keys).Where(member => left.GetValueOrDefault(member) != right.GetValueOrDefault(member)).Order(StringComparer.Ordinal))
                {
                    var outcome = address.Kind == SemanticKind.Specification && member.StartsWith("then", StringComparison.Ordinal);
                    Add(new(outcome ? "specifications" : "members", outcome ? "expected-outcome-changed" : "changed", id, kind, previous, next, member, BeforeHash: Hash(left.GetValueOrDefault(member)), AfterHash: Hash(right.GetValueOrDefault(member))));
                }
            }
        }

        // Fall back to exact authoring keys without fabricating semantic IDs or rename continuity.
        foreach (var key in before.Unassigned.Keys.Union(after.Unassigned.Keys).Order(StringComparer.Ordinal))
        {
            before.Unassigned.TryGetValue(key, out var old);
            after.Unassigned.TryGetValue(key, out var current);
            var kind = (current ?? old!).Kind;
            var start = changes.Count;
            if (old is null || current is null)
            {
                changes.Add(new("declarations", Presence(old, current), null, kind, old?.Address, current?.Address));
                if (kind == "Specification") changes.Add(new("specifications", Presence(old, current), null, kind, old?.Address, current?.Address));
            }
            if (kind == "Event" && old is not null && current is not null)
            {
                Events(null, old.Address, current.Address, [.. old.Parts.OfType<EventSyntax>()], [.. current.Parts.OfType<EventSyntax>()], changes, changedIds);
            }
            var left = old is null ? [] : Members(old.Syntax);
            var right = current is null ? [] : Members(current.Syntax);
            foreach (var member in left.Keys.Union(right.Keys).Where(member => left.GetValueOrDefault(member) != right.GetValueOrDefault(member)).Order(StringComparer.Ordinal))
            {
                var outcome = kind == "Specification" && member.StartsWith("then", StringComparison.Ordinal);
                changes.Add(new(outcome ? "specifications" : "members", outcome ? "expected-outcome-changed" : Presence(old, current), null, kind, old?.Address, current?.Address, member, BeforeHash: Hash(left.GetValueOrDefault(member)), AfterHash: Hash(right.GetValueOrDefault(member))));
            }
            if (changes.Count > start) changes.AddRange(before.AuthoringDependants(key, "before").Concat(after.AuthoringDependants(key, "after")));
        }

        foreach (var id in changedIds.Order(StringComparer.Ordinal))
        {
            changes.AddRange(before.Dependants(id, "before").Concat(after.Dependants(id, "after")));
        }
        IdentityContracts(before, after, changes);

        var sections = _sections.Select(section => new
        {
            section,
            complete = !before.Reasons(section).Concat(after.Reasons(section)).Any(),
            unavailable = before.Reasons(section).Concat(after.Reasons(section)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
        }).ToArray();
        var ordered = changes.Distinct().OrderBy(change => change.Section, StringComparer.Ordinal)
            .ThenBy(change => change.SemanticId, StringComparer.Ordinal).ThenBy(change => change.Kind, StringComparer.Ordinal)
            .ThenBy(change => change.BeforeAddress, StringComparer.Ordinal).ThenBy(change => change.AfterAddress, StringComparer.Ordinal)
            .ThenBy(change => change.ChangeKind, StringComparer.Ordinal).ThenBy(change => change.Member, StringComparer.Ordinal)
            .ThenBy(change => change.Snapshot, StringComparer.Ordinal).ThenBy(change => change.DependantAddress, StringComparer.Ordinal)
            .ThenBy(change => change.Role, StringComparer.Ordinal).ThenBy(change => change.BeforeGeneration).ThenBy(change => change.AfterGeneration).ToArray();
        return new
        {
            sourceRevision = revision,
            beforeRevision = proposal.Before.Revision.ToString(),
            complete = sections.All(section => section.complete),
            hasSemanticChange = SemanticChange(ordered, sections.All(section => section.complete)),
            comparisonLevel = "authoring-structure",
            executableBeforeAvailable = proposal.Before.Compilation.Success,
            executableAfterAvailable = proposal.Workspace.Compilation.Success,
            sections,
            limits = new[] { "Structural authoring comparison, not an equivalence or execution verdict.", "No behavior comparison inside code attachments; use implementation-requirements for content hashes.", "Direct indexed dependants only (before and after); containers and properties aggregate references to their owning/contained declarations. No transitive or runtime impact.", "Unassigned kinds (including constraints) are compared by exact kind/authoring address only, never claimed as identity-preserving renames.", "No revision-to-revision comparison." },
            page = McpPaging.BoundedSourcePage(ordered.Cast<object>(), arguments, revision)
        };

        void Add(Change change)
        {
            changes.Add(change);
            changedIds.Add(change.SemanticId!);
        }
    }

    static void Events(string? id, string previous, string next, EventSyntax[] oldNodes, EventSyntax[] newNodes, List<Change> changes, HashSet<string> changedIds)
    {
        if (oldNodes.Select(node => node.Generation).Distinct().Count() != oldNodes.Length || newNodes.Select(node => node.Generation).Distinct().Count() != newNodes.Length || oldNodes.Concat(newNodes).Any(node => node.Properties.GroupBy(property => property.Name, StringComparer.Ordinal).Any(group => group.Count() > 1))) return;
        var leftEvents = oldNodes.OrderBy(node => node.Generation).ToArray();
        var rightEvents = newNodes.OrderBy(node => node.Generation).ToArray();
        if (leftEvents.Length == 0 || rightEvents.Length == 0) return;
        foreach (var old in leftEvents)
        {
            var current = rightEvents.SingleOrDefault(node => node.Generation == old.Generation);
            if (current is not null)
            {
                Compare(old, current, false);
            }
            else
            {
                changes.Add(new("events", "generation-removed", id, "EventContract", previous, next, ContractBreaking: true, GenerationCovered: false, BeforeGeneration: old.Generation));
                if (id is not null) changedIds.Add(id);
            }
        }
        if (rightEvents[^1].Generation > leftEvents[^1].Generation)
        {
            var old = leftEvents[^1];
            var current = rightEvents[^1];
            var covered = current.HasGenerationMarker && rightEvents.Any(node => node.Generation == old.Generation && Shape(node) == Shape(old));
            Compare(old, current, covered);
        }

        void Compare(EventSyntax old, EventSyntax current, bool covered)
        {
            var left = old.Properties.ToDictionary(property => property.Name, property => SyntaxJson.Serialize(property.Type).GetRawText(), StringComparer.Ordinal);
            var right = current.Properties.ToDictionary(property => property.Name, property => SyntaxJson.Serialize(property.Type).GetRawText(), StringComparer.Ordinal);
            foreach (var property in left.Keys.Union(right.Keys).Where(property => left.GetValueOrDefault(property) != right.GetValueOrDefault(property)).Order(StringComparer.Ordinal))
            {
                var change = (left.ContainsKey(property), right.ContainsKey(property)) switch
                {
                    (false, _) => "property-added",
                    (_, false) => "property-removed",
                    _ => "property-type-changed"
                };
                changes.Add(new("events", change, id, "EventContract", previous, next, property, BeforeType: left.GetValueOrDefault(property), AfterType: right.GetValueOrDefault(property), ContractBreaking: true, GenerationCovered: covered, BeforeGeneration: old.Generation, AfterGeneration: current.Generation));
                if (id is not null) changedIds.Add(id);
            }
        }
    }

    static string Shape(EventSyntax node) => string.Join('|', node.Properties.OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => $"{property.Name}:{SyntaxJson.Serialize(property.Type).GetRawText()}"));

    static Dictionary<string, string> Members(SyntaxNode node) => SyntaxJson.Serialize(node).EnumerateObject()
        .Where(property => !_ignoredMembers.Contains(property.Name, StringComparer.Ordinal))
        .ToDictionary(property => property.Name, property => Normalize(property.Value), StringComparer.Ordinal);

    static string Normalize(JsonElement value)
    {
        var node = JsonNode.Parse(value.GetRawText());
        Strip(node);
        return node?.ToJsonString() ?? "null";
    }

    static void Strip(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(pair => pair.Key).Where(key => _opaqueMembers.Contains(key, StringComparer.Ordinal)).ToArray()) obj.Remove(key);
            foreach (var child in obj.Select(pair => pair.Value)) Strip(child);
        }
        if (node is JsonArray array)
        {
            foreach (var child in array) Strip(child);
        }
    }

    static bool? SemanticChange(Change[] changes, bool complete)
    {
        if (changes.Any(change => _structuralSections.Contains(change.Section, StringComparer.Ordinal) || (change.Section == "declarations" && change.ChangeKind != "moved"))) return true;
        return complete ? false : null;
    }

    static string Presence(object? before, object? after) => (before, after) switch
    {
        (null, _) => "added",
        (_, null) => "removed",
        _ => "changed"
    };

    static string IdentityChange(object? before, object? after) => (before, after) switch
    {
        (null, _) => "assigned",
        (_, null) => "retired",
        _ => "migrated"
    };

    static string Kind(SemanticAddress address) => address.Kind switch { SemanticKind.EventContract => "Event", SemanticKind.CompositeType => "Type", _ => address.Kind.ToString() };

    static SemanticKind[] SectionKinds(string section) => section switch
    {
        "events" => [SemanticKind.EventContract],
        "specifications" => [SemanticKind.Specification],
        _ => [SemanticKind.Command, SemanticKind.ReadModel, SemanticKind.Projection, SemanticKind.Query]
    };

    static string? Hash(string? value) => value is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    static string Address(SemanticAddress address) => string.Join('.', address.Parts.Where(part => part.Kind is not (SemanticAddressPartKind.Application or SemanticAddressPartKind.OwnerKind or SemanticAddressPartKind.Generation)).Select(part => part.Key));

    static void IdentityContracts(Snapshot before, Snapshot after, List<Change> changes)
    {
        var left = before.Workspace.IdentityCatalog.EventContracts.ToDictionary(assignment => assignment.Id.ToString(), StringComparer.Ordinal);
        var right = after.Workspace.IdentityCatalog.EventContracts.ToDictionary(assignment => assignment.Id.ToString(), StringComparer.Ordinal);
        foreach (var id in left.Keys.Union(right.Keys).Order(StringComparer.Ordinal))
        {
            left.TryGetValue(id, out var old);
            right.TryGetValue(id, out var current);
            if (old is null || current is null || !old.Address.Equals(current.Address))
            {
                changes.Add(new("identities", IdentityChange(old, current), null, "EventContractIdentity", old is null ? null : Address(old.Address), current is null ? null : Address(current.Address), EventContractId: id));
            }
        }
    }

    sealed class Snapshot
    {
        readonly McpWorkspaceAnalysis _analysis;
        readonly bool _sourceComplete;
        readonly HashSet<(string Kind, string Address)> _assignedKeys;

        internal Snapshot(ScreenplayWorkspace workspace)
        {
            Workspace = workspace;
            _analysis = McpWorkspaceAnalysis.For(workspace);
            Assignments = workspace.IdentityCatalog.Semantics.ToDictionary(assignment => assignment.Id.ToString(), StringComparer.Ordinal);
            _assignedKeys = [.. Assignments.Values.Select(assignment => (Kind(assignment.Address), Address(assignment.Address)))];
            Nodes = _analysis.Syntax.Entries.Where(entry => entry.SemanticId is not null).GroupBy(entry => entry.SemanticId!.Value.ToString())
                .ToDictionary(group => group.Key, group => group.OrderByDescending(entry => (entry.Node as EventSyntax)?.Generation ?? 0).ToArray(), StringComparer.Ordinal);
            Unassigned = _analysis.Source.Index.Declarations.Where(declaration => !_assignedKeys.Contains((declaration.Kind, declaration.Address)))
                .GroupBy(declaration => $"{declaration.Kind}:{declaration.Address}", StringComparer.Ordinal).Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
            _sourceComplete = !_analysis.Syntax.Diagnostics.Concat(_analysis.Source.Compilation.Diagnostics).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) && _analysis.Syntax.UnresolvedPlacementDocuments.IsEmpty;
        }

        internal ScreenplayWorkspace Workspace { get; }
        internal Dictionary<string, SemanticIdentityAssignment> Assignments { get; }
        internal Dictionary<string, WorkspaceSyntaxEntry[]> Nodes { get; }
        internal Dictionary<string, McpDeclaration> Unassigned { get; }

        internal DocumentLocation[] Documents(string id) => Nodes.TryGetValue(id, out var entries)
            ? [.. entries.Select(entry => Workspace.Documents.Single(document => document.Id == entry.Handle.Document)).Select(document => new DocumentLocation(document.Id.ToString(), document.Path.Value)).Distinct().OrderBy(document => document.DocumentId, StringComparer.Ordinal).ThenBy(document => document.Path, StringComparer.Ordinal)]
            : [];

        internal IEnumerable<string> Reasons(string section)
        {
            if (!_sourceComplete) yield return "Source syntax or import placement is incomplete; missing declarations/members are not evidence of no change.";
            if (_analysis.Source.Index.Declarations.GroupBy(declaration => (declaration.Kind, declaration.Address)).Any(group => group.Count() > 1 && (group.Key.Kind != "Event" || group.SelectMany(declaration => declaration.Parts.OfType<EventSyntax>()).GroupBy(node => node.Generation).Any(generation => generation.Count() > 1))))
                yield return "Duplicate authoring declaration candidates cannot be compared uniquely.";
            if ((section == "members" || section == "declarations" || section == "identities") && Unassigned.Count > 0)
                yield return "Unassigned authoring declarations use exact kind/address keys only; semantic rename/identity continuity cannot be established.";
            if (section == "events" && Unassigned.Values.Any(declaration => declaration.Kind == "Event"))
                yield return "Unassigned event contracts use authoring addresses only; persisted contract identity cannot be compared.";
            if (section == "specifications" && Unassigned.Values.Any(declaration => declaration.Kind == "Specification"))
                yield return "Unassigned specifications use authoring addresses only; specification identity cannot be compared.";
            if (_structuralSections.Contains(section, StringComparer.Ordinal))
            {
                var kinds = SectionKinds(section);
                var count = Assignments.Values.Count(assignment => kinds.Contains(assignment.Address.Kind) && !Comparable(assignment.Id.ToString()));
                if (count > 0) yield return $"{count} catalog declarations have no unique authored structural members available; implicit or ambiguous members cannot be compared.";
            }
            if (section == "declarations" && _analysis.Source.Index.Declarations.Any(declaration => !_assignedKeys.Contains((declaration.Kind, declaration.Address))))
                yield return "Some authoring declarations have no catalog semantic identity and cannot be matched (read-ast/declaration-details retain their syntax).";
            if (section == "dependants" && _analysis.Source.Index.ResolvedReferences.Any(resolution => new McpReferenceEdge(resolution.Reference, resolution.Candidates).Resolution != "resolved"))
                yield return "Dependency index contains unresolved, ambiguous or incomplete references; candidates are reported, not proven runtime dependants.";
        }

        internal IEnumerable<Change> Dependants(string id, string snapshot)
        {
            if (!Assignments.TryGetValue(id, out var assignment)) return [];
            var address = Address(assignment.Address);
            var kinds = Kind(assignment.Address);
            if (assignment.Address.Kind is SemanticKind.Property or SemanticKind.QueryArgument)
            {
                address = address[..address.LastIndexOf('.')];
                kinds = assignment.Address.OwnerKind switch { SemanticKind.EventContract => "Event", SemanticKind.CompositeType => "Type", _ => assignment.Address.OwnerKind.ToString() };
            }
            return Edges(address, kinds, assignment.Address.Kind is SemanticKind.Module or SemanticKind.Feature or SemanticKind.Slice, id, snapshot);
        }

        internal IEnumerable<Change> AuthoringDependants(string key, string snapshot) => Unassigned.TryGetValue(key, out var declaration) ? Edges(declaration.Address, declaration.Kind, false, null, snapshot) : [];

        internal bool Comparable(string id) => Nodes.TryGetValue(id, out var nodes) && (nodes.Length == 1 || nodes.All(entry => entry.Node is ModuleSyntax or FeatureSyntax) || (nodes.All(entry => entry.Node is EventSyntax) && nodes.Select(entry => ((EventSyntax)entry.Node).Generation).Distinct().Count() == nodes.Length));

        IEnumerable<Change> Edges(string address, string kind, bool descendants, string? id, string snapshot)
        {
            var index = _analysis.Source.Index;
            var targets = descendants
                ? index.Declarations.Where(declaration => (declaration.Address == address && declaration.Kind == kind) || declaration.Address.StartsWith($"{address}.", StringComparison.Ordinal))
                : index.Find(address, kind);
            return targets.SelectMany(index.Incoming).Distinct().Where(resolution => resolution.Reference.Owner is not null)
                .Select(resolution => new Change("dependants", "direct", id, kind, snapshot == "before" ? address : null, snapshot == "after" ? address : null, Snapshot: snapshot, DependantAddress: resolution.Reference.Owner!.Address, Role: resolution.Reference.Role, Resolution: new McpReferenceEdge(resolution.Reference, resolution.Candidates).Resolution)).Distinct();
        }
    }

    sealed record DocumentLocation(string DocumentId, string Path);

    sealed record Change(string Section, string ChangeKind, string? SemanticId, string Kind, string? BeforeAddress, string? AfterAddress, string? Member = null,
        string? BeforeHash = null, string? AfterHash = null, string? BeforeType = null, string? AfterType = null, bool? ContractBreaking = null, bool? GenerationCovered = null,
        uint? BeforeGeneration = null, uint? AfterGeneration = null, DocumentLocation[]? BeforeDocuments = null, DocumentLocation[]? AfterDocuments = null,
        string? Snapshot = null, string? DependantAddress = null, string? Role = null, string? Resolution = null, string? EventContractId = null);
}
