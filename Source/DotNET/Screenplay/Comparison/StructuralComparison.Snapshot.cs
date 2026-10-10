// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison;

internal static partial class StructuralComparison
{
    sealed class Snapshot
    {
        readonly AuthoringSnapshot _source;
        readonly WorkspaceSyntaxIndex _syntax;
        readonly bool _sourceComplete;
        readonly bool _matchByAddress;
        readonly bool _declarationLevelOnly;
        readonly Dictionary<string, WorkspaceSyntaxEntry[]> _physical;
        readonly Dictionary<(string Kind, string Address), AuthoredDeclaration[]> _indexed;
        readonly Dictionary<(string Name, SourceLocation Location), SpecificationSyntax> _effectiveSpecifications;
        readonly bool _exampleExpansionFailed;

        internal Snapshot(ScreenplayWorkspace workspace, AuthoringSnapshot? source, WorkspaceSyntaxIndex? syntax, bool matchByAddress, bool declarationLevelOnly)
        {
            Workspace = workspace;
            _matchByAddress = matchByAddress;
            _declarationLevelOnly = declarationLevelOnly;
            _source = source ?? WorkspaceAuthoringAnalysis.For(workspace).Source;
            _syntax = syntax ?? WorkspaceAuthoringAnalysis.For(workspace).Syntax;
            Assignments = declarationLevelOnly ? [] : workspace.IdentityCatalog.Semantics.ToDictionary(assignment => matchByAddress ? AddressKey(assignment.Address) : assignment.Id.ToString(), StringComparer.Ordinal);
            HashSet<(string Kind, string Address)> assignedKeys = [.. Assignments.Values.Select(assignment => (Kind(assignment.Address), Address(assignment.Address)))];
            _physical = _syntax.Entries.Where(entry => entry.SemanticId is not null).GroupBy(entry => entry.SemanticId!.Value.ToString()).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            _indexed = _source.Index.Declarations.GroupBy(declaration => (declaration.Kind, declaration.Address)).ToDictionary(group => group.Key, group => group.ToArray());
            var roots = _source.Compilation.Success && _source.Compilation.Value is { } application
                ? [application]
                : _syntax.Entries.Where(entry => entry.Parent is null).Select(entry => entry.Node).OfType<ApplicationSyntax>().ToArray();
            var expanded = roots.Select(SpecificationExamples.Expand).ToArray();
            _effectiveSpecifications = expanded.SelectMany(value => value.Specifications).GroupBy(value => (value.Authored.Name, value.Authored.Location))
                .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single().Effective);
            _exampleExpansionFailed = expanded.SelectMany(value => value.Diagnostics).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            _indexed[("Application", string.Empty)] = [.. roots.Select(root => new AuthoredDeclaration("Application", string.Empty, [], root.Location, null, null, root))];
            Nodes = Assignments.ToDictionary(pair => pair.Key, pair => FindNodes(pair.Value), StringComparer.Ordinal);
            Unassigned = _indexed.Where(pair => pair.Value.Length > 0 && !assignedKeys.Contains(pair.Key)).ToDictionary(pair => $"{pair.Key.Kind}:{pair.Key.Address}", pair => pair.Value, StringComparer.Ordinal);
            _sourceComplete = !_syntax.Diagnostics.Concat(_source.Compilation.Diagnostics).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) && _syntax.UnresolvedPlacementDocuments.IsEmpty;
        }

        internal ScreenplayWorkspace Workspace { get; }
        internal Dictionary<string, SemanticIdentityAssignment> Assignments { get; }
        internal Dictionary<string, SyntaxNode[]> Nodes { get; }
        internal Dictionary<string, AuthoredDeclaration[]> Unassigned { get; }

        internal DocumentLocation[] Documents(string id)
        {
            var physicalId = Assignments.TryGetValue(id, out var assignment) ? assignment.Id.ToString() : id;
            var paths = (_physical.GetValueOrDefault(physicalId) ?? []).Select(entry => Workspace.Documents.Single(document => document.Id == entry.Handle.Document).Path.Value)
                .Concat((Nodes.GetValueOrDefault(id) ?? []).Select(node => node.Location.Path).OfType<string>()).ToHashSet(StringComparer.Ordinal);
            return [.. Workspace.Documents.Where(document => paths.Contains(document.Path.Value)).Select(document => new DocumentLocation(document.Id.ToString(), document.Path.Value)).OrderBy(document => document.DocumentId, StringComparer.Ordinal).ThenBy(document => document.Path, StringComparer.Ordinal)];
        }

        internal Dictionary<string, string> EffectiveMembers(IEnumerable<SyntaxNode> nodes) => Members(nodes.Select(node => node is SpecificationSyntax specification
            ? _effectiveSpecifications.GetValueOrDefault((specification.Name, specification.Location)) ?? node
            : node));

        internal IEnumerable<StructuralGap> Reasons(string section)
        {
            if (_matchByAddress && section == "identities")
            {
                yield return new(StructuralGapKind.IdentitiesNotCompared, "Persisted identities are not compared when declarations are matched by exact kind and address.");
                yield break;
            }
            if (_declarationLevelOnly && (section == "declarations" || section == "members")) yield return new(StructuralGapKind.DeclarationLevelOnly, "At least one model has no executable model; both sides use authoring declaration keys only, without property-level matching.");
            if (_exampleExpansionFailed && (section == "members" || section == "specifications")) yield return new(StructuralGapKind.ExampleResolution, "Typed example resolution is incomplete; effective specification steps cannot be compared.");
            if (!_sourceComplete) yield return new(StructuralGapKind.IncompleteSource, "Source syntax or import placement is incomplete; missing declarations/members are not evidence of no change.");
            var unavailable = Assignments.Count(pair => !Comparable(pair.Key) && (section == "members" || section == "declarations" || (section == "events" && pair.Value.Address.Kind == SemanticKind.EventContract) || (section == "specifications" && pair.Value.Address.Kind == SemanticKind.Specification)));
            if (unavailable > 0) yield return new(StructuralGapKind.NotComparableAssigned, $"{unavailable} assigned semantic IDs have no unique comparable authored members; every catalog kind is included in this count.");
            var ambiguous = Unassigned.Count(pair => !ComparableAuthoring(pair.Key) && (section == "members" || section == "declarations" || section == "identities" || (section == "events" && pair.Value[0].Kind == "Event") || (section == "specifications" && pair.Value[0].Kind == "Specification")));
            if (ambiguous > 0) yield return new(StructuralGapKind.NotComparableIndexed, $"{ambiguous} indexed kind/address groups have no comparable authored members; ambiguous or unsupported groups were not discarded.");
            if ((section == "members" || section == "declarations" || section == "identities") && Unassigned.Count > 0)
                yield return new(StructuralGapKind.AddressKeysOnly, "Unassigned authoring declarations use exact kind/address keys only; semantic rename/identity continuity cannot be established.");
            if (section == "events" && Unassigned.Values.Any(group => group[0].Kind == "Event"))
                yield return new(StructuralGapKind.UnassignedEventContracts, "Unassigned event contracts use authoring addresses only; persisted contract identity cannot be compared.");
            if (section == "specifications" && Unassigned.Values.Any(group => group[0].Kind == "Specification"))
                yield return new(StructuralGapKind.UnassignedSpecifications, "Unassigned specifications use authoring addresses only; specification identity cannot be compared.");
            if (section == "dependants" && _source.Index.ResolvedReferences.Any(resolution => new ReferenceEdge(resolution.Reference, resolution.Candidates).Resolution != "resolved"))
                yield return new(StructuralGapKind.UnresolvedDependants, "Dependency index contains unresolved, ambiguous or incomplete references; candidates are reported, not proven runtime dependants.");
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
                return _source.Compilation.Success && _source.Compilation.Value is { } application ? [application] : physical;
            }
            var candidates = _indexed.GetValueOrDefault((Kind(assignment.Address), Address(assignment.Address))) ?? [];
            if (kind is SemanticKind.Module or SemanticKind.Feature)
            {
                // LogicalDeclarations exposes merged own members and retains physical locations.
                return _source.Compilation.Success && candidates.Length == 1 ? [candidates[0].Syntax] : physical;
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
            var index = _source.Index;
            var targets = descendants
                ? index.Declarations.Where(declaration => (declaration.Address == address && declaration.Kind == kind) || declaration.Address.StartsWith($"{address}.", StringComparison.Ordinal))
                : index.Find(address, kind);
            var recordKind = id is null ? kind : Kind(Assignments[id].Address);
            return targets.SelectMany(index.Incoming).Distinct().Where(resolution => resolution.Reference.Owner is not null && (!descendants || (resolution.Reference.Owner.Address != address && !resolution.Reference.Owner.Address.StartsWith($"{address}.", StringComparison.Ordinal))))
                .Select(resolution => new Change("dependants", "direct", id, recordKind, snapshot == "before" ? address : null, snapshot == "after" ? address : null, Snapshot: snapshot, DependantAddress: resolution.Reference.Owner!.Address, Role: resolution.Reference.Role, Resolution: new ReferenceEdge(resolution.Reference, resolution.Candidates).Resolution)).Distinct();
        }
    }
}
