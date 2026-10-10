// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison;

internal static partial class StructuralComparison
{
    static readonly string[] _sections = ["declarations", "events", "members", "specifications", "dependants", "identities"];
    static readonly string[] _structuralSections = ["events", "members", "specifications"];
    static readonly string[] _ignoredMembers = ["kind", "name", "description", "documentation", "isPlacement", "fileImports"];
    static readonly string[] _opaqueMembers = ["code", "body", "content", "file"];
    static readonly string[] _hierarchyChildren = ["modules", "concepts", "types", "policies", "personas", "uiProfiles", "themes", "triggers", "layouts", "systems", "eventSources", "screenTemplates", "dialogTemplates", "forms", "features", "slices", "events", "commands", "queries", "projections", "captures", "reactions", "screens", "constraints", "specifications", "readModels", "reducers", "operations"];
    static readonly string[] _limits = ["Structural authoring comparison, not an equivalence or execution verdict.", "Opaque inline content and file references are compared by hash, but behavior inside code and external file contents are not analyzed; use implementation-requirements for attachment content hashes.", "Direct indexed dependants only (before and after); properties use their owner's references and containers aggregate external references to contained declarations, excluding references inside the container. No transitive or runtime impact.", "Unassigned kinds (including constraints) are compared by exact kind/authoring address only, never claimed as identity-preserving renames."];

    internal static StructuralDifference Compare(ScreenplayWorkspace baseline, ScreenplayWorkspace candidate, AuthoringSnapshot? beforeSource = null, WorkspaceSyntaxIndex? beforeSyntax = null, AuthoringSnapshot? afterSource = null, WorkspaceSyntaxIndex? afterSyntax = null)
    {
        var before = new Snapshot(baseline, beforeSource, beforeSyntax);
        var after = new Snapshot(candidate, afterSource, afterSyntax);
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
                Add(new(outcome ? "specifications" : "members", MemberChange(member, left.GetValueOrDefault(member), right.GetValueOrDefault(member), outcome), id, kind, previous, next, member, BeforeHash: Hash(left.GetValueOrDefault(member)), AfterHash: Hash(right.GetValueOrDefault(member)), ContractBreaking: (member == "streamId" || member == "streamIdParts") && StreamSchemaChanged(oldNodes, newNodes) ? true : null));
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
                changes.Add(new(outcome ? "specifications" : "members", MemberChange(member, left.GetValueOrDefault(member), right.GetValueOrDefault(member), outcome), null, kind, previous, next, member, BeforeHash: Hash(left.GetValueOrDefault(member)), AfterHash: Hash(right.GetValueOrDefault(member)), ContractBreaking: (member == "streamId" || member == "streamIdParts") && StreamSchemaChanged(old?.Select(value => value.Syntax) ?? [], current?.Select(value => value.Syntax) ?? []) ? true : null));
            }
            if (changes.Count > start) changes.AddRange(before.AuthoringDependants(key, "before").Concat(after.AuthoringDependants(key, "after")));
        }

        foreach (var id in changedIds.Order(StringComparer.Ordinal))
        {
            changes.AddRange(before.Dependants(id, "before").Concat(after.Dependants(id, "after")));
        }
        IdentityContracts(before, after, changes);

        var sections = _sections.Select(section => new StructuralSection(section, [.. before.Reasons(section).Concat(after.Reasons(section)).Distinct().OrderBy(gap => gap.Statement, StringComparer.Ordinal)])).ToArray();
        var ordered = changes.Distinct().OrderBy(change => change.Section, StringComparer.Ordinal)
            .ThenBy(change => change.SemanticId, StringComparer.Ordinal).ThenBy(change => change.Kind, StringComparer.Ordinal)
            .ThenBy(change => change.BeforeAddress, StringComparer.Ordinal).ThenBy(change => change.AfterAddress, StringComparer.Ordinal)
            .ThenBy(change => change.ChangeKind, StringComparer.Ordinal).ThenBy(change => change.Member, StringComparer.Ordinal)
            .ThenBy(change => change.Snapshot, StringComparer.Ordinal).ThenBy(change => change.DependantAddress, StringComparer.Ordinal)
            .ThenBy(change => change.Role, StringComparer.Ordinal).ThenBy(change => change.BeforeGeneration).ThenBy(change => change.AfterGeneration).ToArray();
        return new(ordered, sections, sections.All(section => section.Complete), SemanticChange(ordered, sections.All(section => section.Complete)), _limits, before.Assignments.Count + before.Unassigned.Count, after.Assignments.Count + after.Unassigned.Count);

        void Add(Change change)
        {
            changes.Add(change);
            changedIds.Add(change.SemanticId!);
        }
    }
}
