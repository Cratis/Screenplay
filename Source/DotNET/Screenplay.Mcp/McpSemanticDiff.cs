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
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// This is a structural review, not the execution/equivalence verdict from decision 0013.
static class McpSemanticDiff
{
    static readonly string[] _sections = ["declarations", "events", "members", "specifications", "dependants", "identities"];
    static readonly string[] _structuralSections = ["events", "members", "specifications"];
    static readonly string[] _ignoredMembers = ["kind", "name", "description", "documentation", "isPlacement", "fileImports"];
    static readonly string[] _opaqueMembers = ["code", "body", "content", "file"];
    static readonly string[] _hierarchyChildren = ["modules", "concepts", "types", "policies", "personas", "uiProfiles", "themes", "triggers", "layouts", "systems", "eventSources", "screenTemplates", "dialogTemplates", "forms", "features", "slices", "events", "commands", "queries", "projections", "captures", "reactions", "screens", "constraints", "specifications", "readModels", "reducers", "operations"];
    static readonly string[] _limits = ["Structural authoring comparison, not an equivalence or execution verdict.", "Opaque inline content and file references are compared by hash, but behavior inside code and external file contents are not analyzed; use implementation-requirements for attachment content hashes.", "Direct indexed dependants only (before and after); properties use their owner's references and containers aggregate external references to contained declarations, excluding references inside the container. No transitive or runtime impact.", "Unassigned kinds (including constraints) are compared by exact kind/authoring address only, never claimed as identity-preserving renames."];

    internal static object Read(IMcpProposal proposal, JsonElement arguments) => Read(proposal.Before, proposal.Workspace, arguments, proposal.Workspace.Revision.ToString(), false);

    internal static object Compare(ScreenplayWorkspace before, ScreenplayWorkspace after, JsonElement arguments) =>
        Read(before, after, arguments, $"comparison:{Hash($"{before.Revision}:{after.Revision}")}", true);

    static object Read(ScreenplayWorkspace baseline, ScreenplayWorkspace candidate, JsonElement arguments, string revision, bool revisions)
    {
        var expected = McpJson.OptionalString(arguments, "expectedSourceRevision");
        if (expected is not null && expected != revision)
        {
            throw new McpFailure(revisions ? "StaleRevision: semantic-diff pages must identify both snapshot revisions." : "StaleRevision: semantic-diff pages must identify the proposal revision.") { FailureKind = revisions ? "StaleRevision" : "RequestFailed" };
        }
        if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 && expected is null)
        {
            throw new McpFailure("'expectedSourceRevision' is required for continuation.", -32602);
        }

        var before = new Snapshot(baseline);
        var after = new Snapshot(candidate);
        var changes = new List<Change>();
        var changedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in before.Assignments.Keys.Union(after.Assignments.Keys).Order(StringComparer.Ordinal))
        {
            before.Assignments.TryGetValue(id, out var old);
            after.Assignments.TryGetValue(id, out var current);
            var address = current?.Address ?? old!.Address;
            var kind = Kind(address);
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
                if (!SameDeclarationLocation(old.Address, current.Address))
                {
                    var change = old.Address.Name != current.Address.Name ? "renamed" : "moved";
                    Add(new("declarations", change, id, kind, previous, next, BeforeDocuments: before.Documents(id), AfterDocuments: after.Documents(id), MoveKind: change == "moved" ? "owner" : null, BeforeOwner: Owner(old.Address), AfterOwner: Owner(current.Address)));
                }
                changes.Add(new("identities", "migrated", id, kind, previous, next));
            }
            var oldNodes = before.Nodes.GetValueOrDefault(id) ?? [];
            var newNodes = after.Nodes.GetValueOrDefault(id) ?? [];
            if (!before.Comparable(id) || !after.Comparable(id)) continue;
            var oldPaths = before.Documents(id);
            var newPaths = after.Documents(id);
            if (!oldPaths.SequenceEqual(newPaths) && (old.Address.Equals(current.Address) || old.Address.Name != current.Address.Name || SameDeclarationLocation(old.Address, current.Address)))
            {
                Add(new("declarations", "moved", id, kind, previous, next, BeforeDocuments: oldPaths, AfterDocuments: newPaths, MoveKind: "document"));
            }

            if (address.Kind == SemanticKind.EventContract)
            {
                Events(id, previous!, next!, [.. oldNodes.OfType<EventSyntax>()], [.. newNodes.OfType<EventSyntax>()], changes, changedIds);
            }
            var left = before.EffectiveMembers(oldNodes);
            var right = after.EffectiveMembers(newNodes);
            foreach (var member in left.Keys.Union(right.Keys).Where(member => left.GetValueOrDefault(member) != right.GetValueOrDefault(member)).Order(StringComparer.Ordinal))
            {
                var outcome = address.Kind == SemanticKind.Specification && member.StartsWith("then", StringComparison.Ordinal);
                Add(new(outcome ? "specifications" : "members", MemberChange(member, left.GetValueOrDefault(member), right.GetValueOrDefault(member), outcome), id, kind, previous, next, member, BeforeHash: Hash(left.GetValueOrDefault(member)), AfterHash: Hash(right.GetValueOrDefault(member))));
            }
        }

        // Fall back to exact authoring keys without fabricating semantic IDs or rename continuity.
        foreach (var key in before.Unassigned.Keys.Union(after.Unassigned.Keys).Order(StringComparer.Ordinal))
        {
            before.Unassigned.TryGetValue(key, out var old);
            after.Unassigned.TryGetValue(key, out var current);
            var declaration = (current ?? old!)[0];
            var kind = declaration.Kind;
            if ((old is not null && !before.ComparableAuthoring(key)) || (current is not null && !after.ComparableAuthoring(key))) continue;
            var previous = old?[0].Address;
            var next = current?[0].Address;
            var start = changes.Count;
            if (old is null || current is null)
            {
                changes.Add(new("declarations", Presence(old, current), null, kind, previous, next));
                if (kind == "Specification") changes.Add(new("specifications", Presence(old, current), null, kind, previous, next));
            }
            if (kind == "Event" && old is not null && current is not null)
            {
                Events(null, previous!, next!, [.. old.Select(value => value.Syntax).OfType<EventSyntax>()], [.. current.Select(value => value.Syntax).OfType<EventSyntax>()], changes, changedIds);
            }
            var left = old is null ? [] : before.EffectiveMembers(old.Select(value => value.Syntax));
            var right = current is null ? [] : after.EffectiveMembers(current.Select(value => value.Syntax));
            foreach (var member in left.Keys.Union(right.Keys).Where(member => left.GetValueOrDefault(member) != right.GetValueOrDefault(member)).Order(StringComparer.Ordinal))
            {
                var outcome = kind == "Specification" && member.StartsWith("then", StringComparison.Ordinal);
                changes.Add(new(outcome ? "specifications" : "members", MemberChange(member, left.GetValueOrDefault(member), right.GetValueOrDefault(member), outcome), null, kind, previous, next, member, BeforeHash: Hash(left.GetValueOrDefault(member)), AfterHash: Hash(right.GetValueOrDefault(member))));
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
            beforeRevision = baseline.Revision.ToString(),
            afterRevision = candidate.Revision.ToString(),
            complete = sections.All(section => section.complete),
            hasSemanticChange = SemanticChange(ordered, sections.All(section => section.complete)),
            comparisonLevel = "authoring-structure",
            executableBeforeAvailable = baseline.Compilation.Success,
            executableAfterAvailable = candidate.Compilation.Success,
            sections,
            limits = revisions ? _limits : [.. _limits, "No revision-to-revision comparison."],
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
                changes.Add(new("events", "generation-removed", id, "Event", previous, next, ContractBreaking: true, GenerationCovered: false, BeforeGeneration: old.Generation));
                if (id is not null) changedIds.Add(id);
            }
        }
        foreach (var current in rightEvents.Where(node => !leftEvents.Any(old => old.Generation == node.Generation)))
        {
            var previousGeneration = rightEvents.LastOrDefault(node => node.Generation < current.Generation);
            var baseline = previousGeneration is null ? null : leftEvents.SingleOrDefault(node => node.Generation == previousGeneration.Generation);
            var covered = previousGeneration is not null && current.HasGenerationMarker && (baseline is null || Shape(baseline) == Shape(previousGeneration));
            changes.Add(new("events", "generation-added", id, "Event", previous, next, GenerationCovered: covered, BeforeGeneration: previousGeneration?.Generation, AfterGeneration: current.Generation));
            if (id is not null) changedIds.Add(id);
            if (previousGeneration is not null) Compare(previousGeneration, current, covered);
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
                changes.Add(new("events", change, id, "Event", previous, next, property, BeforeType: left.GetValueOrDefault(property), AfterType: right.GetValueOrDefault(property), ContractBreaking: true, GenerationCovered: covered, BeforeGeneration: old.Generation, AfterGeneration: current.Generation));
                if (id is not null) changedIds.Add(id);
            }
        }
    }

    static string Shape(EventSyntax node) => string.Join('|', node.Properties.OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => $"{property.Name}:{SyntaxJson.Serialize(property.Type).GetRawText()}"));

    static Dictionary<string, string> Members(IEnumerable<SyntaxNode> nodes) => nodes.SelectMany(node => SyntaxJson.Serialize(node).EnumerateObject()
        .Where(property => !_ignoredMembers.Contains(property.Name, StringComparer.Ordinal) && (!(node is ApplicationSyntax or ModuleSyntax or FeatureSyntax or SliceSyntax) || !_hierarchyChildren.Contains(property.Name, StringComparer.Ordinal)))
        .Select(property => new KeyValuePair<string, string>(node is EventSyntax @event ? $"generation:{@event.Generation}/{property.Name}" : property.Name, Normalize(property.Value))))
        .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    static string MemberChange(string member, string? before, string? after, bool outcome)
    {
        if (Opaque(before, member) != Opaque(after, member)) return "opaque-changed";
        return outcome ? "expected-outcome-changed" : "changed";
    }

    static string Opaque(string? value, string member)
    {
        if (member == "file" || member.EndsWith("/file", StringComparison.Ordinal)) return value ?? "null";
        if (value is null) return "{}";
        var values = new JsonObject();
        Collect(JsonNode.Parse(value), string.Empty);
        return values.ToJsonString();

        void Collect(JsonNode? node, string path)
        {
            if (node is JsonObject obj)
            {
                foreach (var pair in obj)
                {
                    var key = $"{path}/{pair.Key}";
                    if (_opaqueMembers.Contains(pair.Key, StringComparer.Ordinal)) values[key] = pair.Value?.DeepClone();
                    else Collect(pair.Value, key);
                }
            }
            if (node is JsonArray array)
            {
                for (var index = 0; index < array.Count; index++) Collect(array[index], $"{path}/{index}");
            }
        }
    }

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
            foreach (var key in obj.Select(pair => pair.Key).Where(key => key == "description" || key == "documentation").ToArray()) obj.Remove(key);
            foreach (var child in obj.Select(pair => pair.Value)) Strip(child);
        }
        if (node is JsonArray array)
        {
            foreach (var child in array) Strip(child);
        }
    }

    static bool? SemanticChange(Change[] changes, bool complete)
    {
        if (changes.Any(change => _structuralSections.Contains(change.Section, StringComparer.Ordinal) || (change.Section == "declarations" && (change.ChangeKind != "moved" || change.MoveKind == "owner")))) return true;
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

    static string? Hash(string? value) => value is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    static bool SameDeclarationLocation(SemanticAddress before, SemanticAddress after) => before.Kind == after.Kind && before.Parts.Where(part => part.Kind != SemanticAddressPartKind.Generation).SequenceEqual(after.Parts.Where(part => part.Kind != SemanticAddressPartKind.Generation));

    static string Owner(SemanticAddress address) => string.Join('.', address.Parts.SkipLast(1).Where(part => part.Kind is not (SemanticAddressPartKind.Application or SemanticAddressPartKind.OwnerKind or SemanticAddressPartKind.Generation)).Select(part => part.Key));

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
                changes.Add(new("identities", IdentityChange(old, current), null, "Event", old is null ? null : Address(old.Address), current is null ? null : Address(current.Address), EventContractId: id));
            }
        }
    }

    sealed class Snapshot
    {
        readonly McpWorkspaceAnalysis _analysis;
        readonly bool _sourceComplete;
        readonly Dictionary<string, WorkspaceSyntaxEntry[]> _physical;
        readonly Dictionary<(string Kind, string Address), McpDeclaration[]> _indexed;
        readonly Dictionary<(string Name, SourceLocation Location), SpecificationSyntax> _effectiveSpecifications;
        readonly bool _exampleExpansionFailed;

        internal Snapshot(ScreenplayWorkspace workspace)
        {
            Workspace = workspace;
            _analysis = McpWorkspaceAnalysis.For(workspace);
            Assignments = workspace.IdentityCatalog.Semantics.ToDictionary(assignment => assignment.Id.ToString(), StringComparer.Ordinal);
            HashSet<(string Kind, string Address)> assignedKeys = [.. Assignments.Values.Select(assignment => (Kind(assignment.Address), Address(assignment.Address)))];
            _physical = _analysis.Syntax.Entries.Where(entry => entry.SemanticId is not null).GroupBy(entry => entry.SemanticId!.Value.ToString()).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            _indexed = _analysis.Source.Index.Declarations.GroupBy(declaration => (declaration.Kind, declaration.Address)).ToDictionary(group => group.Key, group => group.ToArray());
            var roots = _analysis.Source.Compilation.Success && _analysis.Source.Compilation.Value is { } application
                ? [application]
                : _analysis.Syntax.Entries.Where(entry => entry.Parent is null).Select(entry => entry.Node).OfType<ApplicationSyntax>().ToArray();
            var expanded = roots.Select(SpecificationExamples.Expand).ToArray();
            _effectiveSpecifications = expanded.SelectMany(value => value.Specifications).GroupBy(value => (value.Authored.Name, value.Authored.Location))
                .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single().Effective);
            _exampleExpansionFailed = expanded.SelectMany(value => value.Diagnostics).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            _indexed[("Application", string.Empty)] = [.. roots.Select(root => new McpDeclaration("Application", string.Empty, [], root.Location, null, null, root))];
            Nodes = Assignments.ToDictionary(pair => pair.Key, pair => FindNodes(pair.Value), StringComparer.Ordinal);
            Unassigned = _indexed.Where(pair => pair.Value.Length > 0 && !assignedKeys.Contains(pair.Key)).ToDictionary(pair => $"{pair.Key.Kind}:{pair.Key.Address}", pair => pair.Value, StringComparer.Ordinal);
            _sourceComplete = !_analysis.Syntax.Diagnostics.Concat(_analysis.Source.Compilation.Diagnostics).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) && _analysis.Syntax.UnresolvedPlacementDocuments.IsEmpty;
        }

        internal ScreenplayWorkspace Workspace { get; }
        internal Dictionary<string, SemanticIdentityAssignment> Assignments { get; }
        internal Dictionary<string, SyntaxNode[]> Nodes { get; }
        internal Dictionary<string, McpDeclaration[]> Unassigned { get; }

        internal DocumentLocation[] Documents(string id)
        {
            var paths = (_physical.GetValueOrDefault(id) ?? []).Select(entry => Workspace.Documents.Single(document => document.Id == entry.Handle.Document).Path.Value)
                .Concat((Nodes.GetValueOrDefault(id) ?? []).Select(node => node.Location.Path).OfType<string>()).ToHashSet(StringComparer.Ordinal);
            return [.. Workspace.Documents.Where(document => paths.Contains(document.Path.Value)).Select(document => new DocumentLocation(document.Id.ToString(), document.Path.Value)).OrderBy(document => document.DocumentId, StringComparer.Ordinal).ThenBy(document => document.Path, StringComparer.Ordinal)];
        }

        internal Dictionary<string, string> EffectiveMembers(IEnumerable<SyntaxNode> nodes) => Members(nodes.Select(node => node is SpecificationSyntax specification
            ? _effectiveSpecifications.GetValueOrDefault((specification.Name, specification.Location)) ?? node
            : node));

        internal IEnumerable<string> Reasons(string section)
        {
            if (_exampleExpansionFailed && (section == "members" || section == "specifications")) yield return "Typed example resolution is incomplete; effective specification steps cannot be compared.";
            if (!_sourceComplete) yield return "Source syntax or import placement is incomplete; missing declarations/members are not evidence of no change.";
            var unavailable = Assignments.Values.Count(assignment => !Comparable(assignment.Id.ToString()) && (section == "members" || section == "declarations" || (section == "events" && assignment.Address.Kind == SemanticKind.EventContract) || (section == "specifications" && assignment.Address.Kind == SemanticKind.Specification)));
            if (unavailable > 0) yield return $"{unavailable} assigned semantic IDs have no unique comparable authored members; every catalog kind is included in this count.";
            var ambiguous = Unassigned.Count(pair => !ComparableAuthoring(pair.Key) && (section == "members" || section == "declarations" || section == "identities" || (section == "events" && pair.Value[0].Kind == "Event") || (section == "specifications" && pair.Value[0].Kind == "Specification")));
            if (ambiguous > 0) yield return $"{ambiguous} indexed kind/address groups have no comparable authored members; ambiguous or unsupported groups were not discarded.";
            if ((section == "members" || section == "declarations" || section == "identities") && Unassigned.Count > 0)
                yield return "Unassigned authoring declarations use exact kind/address keys only; semantic rename/identity continuity cannot be established.";
            if (section == "events" && Unassigned.Values.Any(group => group[0].Kind == "Event"))
                yield return "Unassigned event contracts use authoring addresses only; persisted contract identity cannot be compared.";
            if (section == "specifications" && Unassigned.Values.Any(group => group[0].Kind == "Specification"))
                yield return "Unassigned specifications use authoring addresses only; specification identity cannot be compared.";
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

        internal IEnumerable<Change> AuthoringDependants(string key, string snapshot) => Unassigned.TryGetValue(key, out var declarations) ? Edges(declarations[0].Address, declarations[0].Kind, declarations[0].Kind == "Module" || declarations[0].Kind == "Feature" || declarations[0].Kind == "Slice", null, snapshot) : [];

        internal bool Comparable(string id) => Nodes.TryGetValue(id, out var nodes) && ComparableNodes(nodes);

        internal bool ComparableAuthoring(string key) => Unassigned.TryGetValue(key, out var declarations) && declarations.All(declaration => !declaration.IsImplicit) && ComparableNodes([.. declarations.Select(declaration => declaration.Syntax)]);

        bool ComparableNodes(SyntaxNode[] nodes) => nodes.Length > 0 && !(_exampleExpansionFailed && nodes.Any(node => node is SpecificationSyntax)) && (nodes.Length == 1 || nodes.All(node => node is EventSyntax)) &&
            (!nodes.Any(node => node is EventSyntax) || (nodes.All(node => node is EventSyntax) && nodes.OfType<EventSyntax>().Select(node => node.Generation).Distinct().Count() == nodes.Length && nodes.OfType<EventSyntax>().All(node => node.Properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == node.Properties.Count())));

        SyntaxNode[] FindNodes(SemanticIdentityAssignment assignment)
        {
            var physical = (_physical.GetValueOrDefault(assignment.Id.ToString()) ?? []).Select(entry => entry.Node).ToArray();
            var kind = assignment.Address.Kind;
            if (kind == SemanticKind.Application)
            {
                // The source assembly owns merged root meaning. Never compare just one fragment.
                return _analysis.Source.Compilation.Success && _analysis.Source.Compilation.Value is { } application ? [application] : physical;
            }
            var candidates = _indexed.GetValueOrDefault((Kind(assignment.Address), Address(assignment.Address))) ?? [];
            if (kind is SemanticKind.Module or SemanticKind.Feature)
            {
                // McpLogicalDeclarations exposes merged own members and retains physical locations.
                return _analysis.Source.Compilation.Success && candidates.Length == 1 ? [candidates[0].Syntax] : physical;
            }
            if (physical.Length > 0) return physical;
            if (kind == SemanticKind.Property && assignment.Address.OwnerKind == SemanticKind.Trigger)
            {
                var owner = Owner(assignment.Address);
                var triggers = _indexed.GetValueOrDefault(("Trigger", owner)) ?? [];
                return triggers.Length == 1 && triggers[0].Syntax is TriggerSyntax trigger ? [.. trigger.Data.Where(data => data.Name == assignment.Address.Name)] : [];
            }
            return candidates.All(candidate => !candidate.IsImplicit) ? [.. candidates.Select(candidate => candidate.Syntax)] : [];
        }

        IEnumerable<Change> Edges(string address, string kind, bool descendants, string? id, string snapshot)
        {
            var index = _analysis.Source.Index;
            var targets = descendants
                ? index.Declarations.Where(declaration => (declaration.Address == address && declaration.Kind == kind) || declaration.Address.StartsWith($"{address}.", StringComparison.Ordinal))
                : index.Find(address, kind);
            var recordKind = id is null ? kind : Kind(Assignments[id].Address);
            return targets.SelectMany(index.Incoming).Distinct().Where(resolution => resolution.Reference.Owner is not null && (!descendants || (resolution.Reference.Owner.Address != address && !resolution.Reference.Owner.Address.StartsWith($"{address}.", StringComparison.Ordinal))))
                .Select(resolution => new Change("dependants", "direct", id, recordKind, snapshot == "before" ? address : null, snapshot == "after" ? address : null, Snapshot: snapshot, DependantAddress: resolution.Reference.Owner!.Address, Role: resolution.Reference.Role, Resolution: new McpReferenceEdge(resolution.Reference, resolution.Candidates).Resolution)).Distinct();
        }
    }

    sealed record DocumentLocation(string DocumentId, string Path);

    sealed record Change(string Section, string ChangeKind, string? SemanticId, string Kind, string? BeforeAddress, string? AfterAddress, string? Member = null,
        string? BeforeHash = null, string? AfterHash = null, string? BeforeType = null, string? AfterType = null, bool? ContractBreaking = null, bool? GenerationCovered = null,
        uint? BeforeGeneration = null, uint? AfterGeneration = null, DocumentLocation[]? BeforeDocuments = null, DocumentLocation[]? AfterDocuments = null,
        string? Snapshot = null, string? DependantAddress = null, string? Role = null, string? Resolution = null, string? EventContractId = null,
        string? MoveKind = null, string? BeforeOwner = null, string? AfterOwner = null);
}
