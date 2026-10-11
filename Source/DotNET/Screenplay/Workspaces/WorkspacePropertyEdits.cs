// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertyEdits(
    WorkspaceSyntaxIndex index,
    WorkspacePropertyPlan plan,
    IReadOnlyList<WorkspacePropertyPlan.PropertyMember> selected,
    WorkspacePropertyRequest request)
{
    readonly Dictionary<DocumentId, JsonNode> _roots = index.Entries.Where(entry => entry.Parent is null).ToDictionary(entry => entry.Handle.Document, entry => WorkspaceSyntaxMutation.Json(entry.Node));
    readonly HashSet<DocumentId> _touched = [];
    readonly HashSet<WorkspaceNodeHandle> _removed = [];
    readonly List<WorkspacePropertyChange> _changes = [];
    readonly Dictionary<SemanticAddress, SemanticAddress> _renames = [];
    readonly List<SemanticAddress> _retirements = [];
    readonly HashSet<(WorkspaceNodeHandle, string)> _autoMaps = [];
    readonly WorkspacePropertyAssertionProvenance _assertions = new(index);

    internal IReadOnlyDictionary<SemanticAddress, SemanticAddress> Renames => _renames;
    internal ImmutableArray<SemanticAddress> Retirements => [.. _retirements];

    internal ImmutableArray<WorkspacePropertyChange> Apply()
    {
        MoveInitialProperties();
        foreach (var member in selected) ChangeMember(member);
        foreach (var handle in _removed.OrderByDescending(handle => handle.Path.Count(character => character == '/')).ThenByDescending(handle => int.Parse(handle.Path[(handle.Path.LastIndexOf('/') + 1)..], System.Globalization.CultureInfo.InvariantCulture))) Remove(handle);
        return [.. _changes.Distinct().OrderBy(change => change.Address, WorkspaceSemanticAddressComparer.Instance).ThenBy(change => change.Location.Path, StringComparer.Ordinal).ThenBy(change => change.Location.Line).ThenBy(change => change.Location.Column).ThenBy(change => change.Role, StringComparer.Ordinal)];
    }

    internal WorkspacePropertyRefusal? InspectProjectionContinuity(ScreenplayWorkspace candidate) => new WorkspacePropertyProjectionContinuity(plan, request, selected).Inspect(candidate);

    internal ImmutableArray<WorkspaceOperation> Documents(ImmutableArray<WorkspacePropertyGeneration> generations)
    {
        // Clone the fully edited current shape once per selected event owner. Restore the exact original
        // generation before appending; other generations and all their property identities stay unchanged.
        foreach (var generation in generations.Where(generation => generation.Mode == "newGeneration")) AppendGeneration(generation);
        return [.. _touched.OrderBy(document => document.ToString(), StringComparer.Ordinal).Select(document => new ReplaceWorkspaceSyntaxDocument(document, WorkspaceSyntaxMutation.Syntax(_roots[document])))];
    }

    static bool Writer(string role) => role switch { "mapping-target" or "fixture" or "assertion" => true, _ => false };
    static bool Reader(string role) => role switch { "mapping-source" or "key" or "condition" or "response" => true, _ => false };
    static bool Fixture(string role) => role switch { "fixture" or "assertion" => true, _ => false };

    void MoveInitialProperties()
    {
        foreach (var owner in selected.Where(member => request.NewGeneration && member.Owner.Node is EventSyntax).Select(member => member.Owner).DistinctBy(owner => owner.Address)) MoveOwnerProperties(owner);
    }

    void MoveOwnerProperties(WorkspaceSyntaxEntry owner)
    {
        foreach (var property in index.Entries.Where(entry => entry.Parent == owner.Handle)
            .Where(entry => entry.Address is { } address && address.Parts[^3].Kind != SemanticAddressPartKind.Generation))
        {
            _renames[property.Address!] = SemanticAddress.ForEventProperty(owner.Address!, EventContractRevision.Initial, property.Address!.Name);
        }
    }

    void ChangeMember(WorkspacePropertyPlan.PropertyMember member)
    {
        ChangeDeclaration(member);
        foreach (var link in plan.Links.Incoming(member.Declaration)) ChangeReference(member, link);
    }

    void AppendGeneration(WorkspacePropertyGeneration generation)
    {
        var owner = selected.First(member => member.Owner.Address!.Equals(generation.Event)).Owner;
        var current = WorkspaceSyntaxMutation.At(_roots[owner.Handle.Document], owner.Handle.Path);
        var next = current.DeepClone();
        next["generation"] = generation.To;
        next["hasGenerationMarker"] = true;
        var separator = owner.Handle.Path.LastIndexOf('/');
        var collection = (JsonArray)WorkspaceSyntaxMutation.At(_roots[owner.Handle.Document], owner.Handle.Path[..separator]);
        collection[int.Parse(owner.Handle.Path[(separator + 1)..], System.Globalization.CultureInfo.InvariantCulture)] = WorkspaceSyntaxMutation.Json(owner.Node);
        collection.Add(next);
    }

    void ChangeDeclaration(WorkspacePropertyPlan.PropertyMember member)
    {
        var advances = member.Owner.Node is EventSyntax && request.NewGeneration;
        if (request.Action == "rename") RenameDeclaration(member, advances);
        else RemoveDeclaration(member, advances);
        Record(new(member, "declaration", member.Entry.Location), request.Action == "rename" ? "renamed" : "removed");
    }

    void RenameDeclaration(WorkspacePropertyPlan.PropertyMember member, bool advances)
    {
        Set(new(member.Entry.Handle, "name", request.NewName!));
        if (advances) return;
        var address = member.Entry.Address!;
        var parts = address.Parts.SetItem(address.Parts.Length - 1, address.Parts[^1] with { Key = request.NewName! });
        _renames[address] = SemanticAddress.FromCanonical(address.Kind, parts);
    }

    void RemoveDeclaration(WorkspacePropertyPlan.PropertyMember member, bool advances)
    {
        _removed.Add(member.Entry.Handle);
        _touched.Add(member.Entry.Handle.Document);
        if (!advances && member.Entry.SemanticId is not null) _retirements.Add(member.Entry.Address!);
    }

    void ChangeReference(WorkspacePropertyPlan.PropertyMember member, WorkspacePropertyLink link)
    {
        var source = new ChangeSource(member, link.Reference.Role, link.Reference.Location);
        if (link.Reference.Role == "automap")
        {
            AutoMap(source, link);
            return;
        }
        var occurrence = index.Find(link.Occurrence)!;

        // Case-row values belong to a shared table parameter, not to a property. Keep them for
        // unselected consumers; only the property's own parameter-use mapping is changed.
        if (WorkspacePropertyPlan.Parents(occurrence, index).Any(parent => parent.Kind == "SpecificationCaseSyntax")) return;
        if (request.Action == "remove")
        {
            RemoveReference(occurrence, source);
            return;
        }
        RenameReference(occurrence, link);
        Record(source, Fixture(link.Reference.Role) ? "fixtureRewritten" : "mappingRewritten");
    }

    void RemoveReference(WorkspaceSyntaxEntry occurrence, ChangeSource source)
    {
        if (!Writer(source.Role)) return;
        _removed.Add(occurrence.Handle);
        _touched.Add(occurrence.Handle.Document);
        Record(source, source.Role == "mapping-target" ? "removed" : "fixtureRemoved", source.Role == "assertion");
        foreach (var consumer in _assertions.Consumers(occurrence)) Record(source with { Role = "assertion", Location = consumer }, "fixtureRemoved", true);
    }

    void RenameReference(WorkspaceSyntaxEntry occurrence, WorkspacePropertyLink link)
    {
        if (Writer(link.Reference.Role))
        {
            Set(new(occurrence.Handle, "property", request.NewName!));
            return;
        }
        if (Reader(link.Reference.Role))
        {
            new WorkspacePropertyReaders(index, request.NewName, Set).Rewrite(occurrence, link);
            return;
        }
        throw new InvalidWorkspaceAuthoring($"UnsupportedPropertySpan: {link.Reference.Role} at {link.Reference.Location} requires an explicit edit.");
    }

    void AutoMap(ChangeSource change, WorkspacePropertyLink link)
    {
        if (request.Action != "rename") return;
        var member = change.Member;
        var counterpart = plan.Member(link.OtherEnd!)!;
        var target = member.Kind == "ReadModel" ? member : counterpart;
        var source = member.Kind == "Event" ? member : counterpart;
        var targetName = SelectedName(target.Declaration);
        var sourceName = SelectedName(source.Declaration);
        if (targetName.Equals(sourceName, StringComparison.OrdinalIgnoreCase) || !_autoMaps.Add((plan.MappingOwner(link.Occurrence).Handle, target.Declaration.Address))) return;
        InsertMapping(link, targetName, sourceName);
        Record(change, "explicitMappingInserted");
    }

    void InsertMapping(WorkspacePropertyLink link, string target, string source)
    {
        var occurrence = plan.MappingOwner(link.Occurrence);
        var mapping = new SetMappingSyntax(target, new PathExpressionSyntax(source, occurrence.Location), occurrence.Location);
        var node = WorkspaceSyntaxMutation.At(_roots[occurrence.Handle.Document], occurrence.Handle.Path);
        if (node["mappings"] is not JsonArray mappings) throw new InvalidWorkspaceAuthoring($"UnsupportedPropertySpan: AutoMap at {link.Reference.Location} has no direct mapping slot.");
        mappings.Add(WorkspaceSyntaxMutation.Json(mapping));
        _touched.Add(occurrence.Handle.Document);
    }

    string SelectedName(AuthoredDeclaration declaration) => selected.Any(member => member.Declaration == declaration) ? request.NewName! : declaration.Name;

    void Set(WorkspacePropertyMutation mutation)
    {
        WorkspaceSyntaxMutation.Set(_roots[mutation.Target.Document], $"{mutation.Target.Path}/{mutation.Member}", mutation.Value);
        _touched.Add(mutation.Target.Document);
    }

    void Remove(WorkspaceNodeHandle handle)
    {
        var separator = handle.Path.LastIndexOf('/');
        var parent = WorkspaceSyntaxMutation.At(_roots[handle.Document], handle.Path[..separator]);
        if (parent is not JsonArray collection) throw new InvalidWorkspaceAuthoring("UnsupportedPropertySpan: removal requires one direct collection member.");
        collection.RemoveAt(int.Parse(handle.Path[(separator + 1)..], System.Globalization.CultureInfo.InvariantCulture));
    }

    void Record(ChangeSource source, string change, bool weakens = false) => _changes.Add(new(source.Member.Entry.Address!, source.Member.Kind, source.Role, change, source.Location, weakens));

    sealed record ChangeSource(WorkspacePropertyPlan.PropertyMember Member, string Role, SourceLocation Location);
}
