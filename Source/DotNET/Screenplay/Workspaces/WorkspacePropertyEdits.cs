// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
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

    internal IReadOnlyDictionary<SemanticAddress, SemanticAddress> Renames => _renames;
    internal ImmutableArray<SemanticAddress> Retirements => [.. _retirements];

    internal ImmutableArray<WorkspacePropertyChange> Apply()
    {
        foreach (var owner in selected.Where(member => request.NewGeneration && member.Owner.Node is EventSyntax).Select(member => member.Owner).DistinctBy(owner => owner.Address))
        {
            foreach (var property in index.Entries.Where(entry => entry.Parent == owner.Handle && entry.Address is { } address && address.Parts[^3].Kind != SemanticAddressPartKind.Generation))
            {
                _renames[property.Address!] = SemanticAddress.ForEventProperty(owner.Address!, EventContractRevision.Initial, property.Address!.Name);
            }
        }
        foreach (var member in selected)
        {
            ChangeDeclaration(member);
            foreach (var link in plan.Links.Incoming(member.Declaration)) ChangeReference(member, link);
        }
        foreach (var handle in _removed.OrderByDescending(handle => handle.Path.Count(character => character == '/')).ThenByDescending(handle => int.Parse(handle.Path[(handle.Path.LastIndexOf('/') + 1)..], System.Globalization.CultureInfo.InvariantCulture)))
        {
            Remove(handle);
        }
        return [.. _changes.Distinct().OrderBy(change => change.Address, WorkspaceSemanticAddressComparer.Instance).ThenBy(change => change.Location.Path, StringComparer.Ordinal).ThenBy(change => change.Location.Line).ThenBy(change => change.Location.Column).ThenBy(change => change.Role, StringComparer.Ordinal)];
    }

    internal ImmutableArray<WorkspaceOperation> Documents(ImmutableArray<WorkspacePropertyGeneration> generations)
    {
        // Clone the fully edited current shape once per selected event owner. Restore the exact original
        // generation before appending; other generations and all their property identities stay unchanged.
        foreach (var generation in generations.Where(generation => generation.Mode == "newGeneration"))
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
        return [.. _touched.OrderBy(document => document.ToString(), StringComparer.Ordinal).Select(document => new ReplaceWorkspaceSyntaxDocument(document, WorkspaceSyntaxMutation.Syntax(_roots[document])))];
    }

    static bool Writer(string role) => role switch { "mapping-target" or "fixture" or "assertion" => true, _ => false };
    static bool Reader(string role) => role switch { "mapping-source" or "key" or "condition" or "response" => true, _ => false };
    static bool Fixture(string role) => role switch { "fixture" or "assertion" => true, _ => false };
    static string? SourcePath(SyntaxNode node) => node switch
    {
        PathExpressionSyntax expression => expression.Path,
        ContextExpressionSyntax expression when expression.Path.StartsWith("command.", StringComparison.Ordinal) => expression.Path[8..],
        SourceItemExpressionSyntax expression => $"$.{expression.Path}",
        _ => null
    };

    void ChangeDeclaration(WorkspacePropertyPlan.PropertyMember member)
    {
        var address = member.Entry.Address!;
        var eventAdvances = member.Owner.Node is EventSyntax && request.NewGeneration;
        if (request.Action == "rename")
        {
            Set(member.Entry.Handle, "name", request.NewName!);
            if (!eventAdvances)
            {
                var parts = address.Parts.SetItem(address.Parts.Length - 1, address.Parts[^1] with { Key = request.NewName! });
                _renames[address] = SemanticAddress.FromCanonical(address.Kind, parts);
            }
        }
        else
        {
            _removed.Add(member.Entry.Handle);
            _touched.Add(member.Entry.Handle.Document);
            if (!eventAdvances && member.Entry.SemanticId is not null) _retirements.Add(address);
        }
        Record(member, "declaration", request.Action == "rename" ? "renamed" : "removed", member.Entry.Location);
    }

    void ChangeReference(WorkspacePropertyPlan.PropertyMember member, WorkspacePropertyLink link)
    {
        if (link.Reference.Role == "automap")
        {
            AutoMap(member, link);
            return;
        }
        var occurrence = index.Find(link.Occurrence)!;

        // Case-row values belong to a shared table parameter, not to a property. Keep them for
        // unselected consumers; only the property's own parameter-use mapping is changed.
        if (WorkspacePropertyPlan.Parents(occurrence, index).Any(parent => parent.Kind == "SpecificationCaseSyntax")) return;
        if (request.Action == "remove")
        {
            if (Writer(link.Reference.Role))
            {
                _removed.Add(occurrence.Handle);
                _touched.Add(occurrence.Handle.Document);
                Record(member, link.Reference.Role, link.Reference.Role == "mapping-target" ? "removed" : "fixtureRemoved", link.Reference.Location, link.Reference.Role == "assertion");
            }
            return;
        }
        if (Writer(link.Reference.Role))
        {
            Set(occurrence.Handle, "property", request.NewName!);
        }
        else if (Reader(link.Reference.Role))
        {
            RewriteReader(occurrence, link);
        }
        else
        {
            throw new InvalidWorkspaceAuthoring($"UnsupportedPropertySpan: {link.Reference.Role} at {link.Reference.Location} requires an explicit edit.");
        }
        Record(member, link.Reference.Role, Fixture(link.Reference.Role) ? "fixtureRewritten" : "mappingRewritten", link.Reference.Location);
    }

    void RewriteReader(WorkspaceSyntaxEntry occurrence, WorkspacePropertyLink link)
    {
        var expressions = index.Entries.Where(entry => SourcePath(entry.Node) == link.Reference.Name &&
            (entry.Handle == occurrence.Handle || WorkspacePropertyPlan.Parents(entry, index).Any(parent => parent.Handle == occurrence.Handle))).ToArray();
        if (expressions.Length > 0)
        {
            foreach (var expression in expressions)
            {
                var path = expression.Node switch
                {
                    PathExpressionSyntax value => value.Path,
                    ContextExpressionSyntax value => value.Path,
                    SourceItemExpressionSyntax value => value.Path,
                    _ => string.Empty
                };
                Set(expression.Handle, "path", RenamedPath(path));
            }
            return;
        }
        if (occurrence.Node is CaptureWhenSyntax captureWhen)
        {
            RewriteNames(occurrence.Handle, "properties", captureWhen.Properties, link.Reference.Name);
            return;
        }
        if (occurrence.Node is UniquePropertyConstraintSyntax constraint && constraint.Property != link.Reference.Name)
        {
            RewriteNames(occurrence.Handle, "additionalProperties", constraint.AdditionalProperties, link.Reference.Name);
            return;
        }
        var member = occurrence.Node switch
        {
            ComparisonConditionSyntax => "left",
            ValidationRuleSyntax or PropertyResponseSourceSyntax or UniquePropertyConstraintSyntax => "property",
            QueryParameterSyntax => "name",
            CaptureSyntax => "key",
            _ => null
        } ?? throw new InvalidWorkspaceAuthoring($"UnsupportedPropertySpan: {occurrence.Kind} at {link.Reference.Location} cannot be rewritten safely.");
        Set(occurrence.Handle, member, RenamedPath(link.Reference.Name));
    }

    void AutoMap(WorkspacePropertyPlan.PropertyMember member, WorkspacePropertyLink link)
    {
        if (request.Action != "rename") return;
        var counterpart = plan.Member(link.OtherEnd!)!;
        var target = member.Kind == "ReadModel" ? member : counterpart;
        var source = member.Kind == "Event" ? member : counterpart;
        var targetName = SelectedName(target.Declaration);
        var sourceName = SelectedName(source.Declaration);
        if (targetName.Equals(sourceName, StringComparison.OrdinalIgnoreCase) || !_autoMaps.Add((link.Occurrence, target.Declaration.Address))) return;
        var occurrence = plan.MappingOwner(link.Occurrence);
        var mapping = new SetMappingSyntax(targetName, new PathExpressionSyntax(sourceName, occurrence.Location), occurrence.Location);
        var node = WorkspaceSyntaxMutation.At(_roots[occurrence.Handle.Document], occurrence.Handle.Path);
        if (node["mappings"] is not JsonArray mappings) throw new InvalidWorkspaceAuthoring($"UnsupportedPropertySpan: AutoMap at {link.Reference.Location} has no direct mapping slot.");
        mappings.Add(WorkspaceSyntaxMutation.Json(mapping));
        _touched.Add(occurrence.Handle.Document);
        Record(member, "automap", "explicitMappingInserted", link.Reference.Location);
    }

    string RenamedPath(string path) => path[..(path.LastIndexOf('.') + 1)] + request.NewName;

    void RewriteNames(WorkspaceNodeHandle handle, string member, IEnumerable<string> names, string expected)
    {
        foreach (var (_, ordinal) in names.Select((name, ordinal) => (name, ordinal)).Where(value => value.name == expected))
        {
            Set(handle, $"{member}/{ordinal}", request.NewName!);
        }
    }

    string SelectedName(AuthoredDeclaration declaration) => selected.Any(member => member.Declaration == declaration) ? request.NewName! : declaration.Name;

    void Set(WorkspaceNodeHandle handle, string member, string value)
    {
        WorkspaceSyntaxMutation.Set(_roots[handle.Document], $"{handle.Path}/{member}", value);
        _touched.Add(handle.Document);
    }

    void Remove(WorkspaceNodeHandle handle)
    {
        var separator = handle.Path.LastIndexOf('/');
        var parent = WorkspaceSyntaxMutation.At(_roots[handle.Document], handle.Path[..separator]);
        if (parent is not JsonArray collection) throw new InvalidWorkspaceAuthoring("UnsupportedPropertySpan: removal requires one direct collection member.");
        collection.RemoveAt(int.Parse(handle.Path[(separator + 1)..], System.Globalization.CultureInfo.InvariantCulture));
    }

    void Record(WorkspacePropertyPlan.PropertyMember member, string role, string change, Diagnostics.SourceLocation location, bool weakens = false) =>
        _changes.Add(new(member.Entry.Address!, member.Kind, role, change, location, weakens));
}
