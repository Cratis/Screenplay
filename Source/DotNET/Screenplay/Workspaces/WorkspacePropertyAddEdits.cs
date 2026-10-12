// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertyAddEdits(WorkspaceSyntaxIndex index, WorkspacePropertyPlan properties, WorkspacePropertyAddPlan plan, WorkspacePropertyRequest request)
{
    readonly Dictionary<DocumentId, JsonNode> _roots = index.Entries.Where(entry => entry.Parent is null).ToDictionary(entry => entry.Handle.Document, entry => WorkspaceSyntaxMutation.Json(entry.Node));
    readonly HashSet<DocumentId> _touched = [];
    readonly List<WorkspacePropertyChange> _changes = [];

    internal IReadOnlyDictionary<SemanticAddress, SemanticAddress> Renames => plan.Additions.Where(addition => request.NewGeneration && addition.Owner.Node is EventSyntax)
        .SelectMany(addition => index.Entries.Where(entry => entry.Parent == addition.Owner.Handle && entry.Address is { } address && address.Parts[^3].Kind != SemanticAddressPartKind.Generation))
        .ToDictionary(entry => entry.Address!, entry => SemanticAddress.ForEventProperty(index.Find(entry.Parent!)!.Address!, EventContractRevision.Initial, entry.Address!.Name));

    internal WorkspacePropertyReport Describe(ScreenplayWorkspace candidate, WorkspacePropertyReport report)
    {
        var after = new WorkspacePropertyPlan(WorkspaceSyntaxIndex.Create(candidate));
        var members = plan.Additions.ToDictionary(WorkspacePropertyAddPlan.Address, addition => after.Members.Single(member => member.Declaration.Address == $"{addition.AuthoredAddress}.{addition.Name}"));
        var target = members[WorkspacePropertyAddPlan.Address(plan.Additions[0])];
        return report with
        {
            Target = target.Target,
            Changed = [.. _changes.Select(change => change with { Address = members[change.Address].Entry.Address! }).OrderBy(change => change.Address, WorkspaceSemanticAddressComparer.Instance)
                .ThenBy(change => change.Location.Path, StringComparer.Ordinal).ThenBy(change => change.Location.Line).ThenBy(change => change.Role, StringComparer.Ordinal)]
        };
    }

    internal WorkspacePropertyRefusal? Apply()
    {
        foreach (var addition in plan.Additions) Insert(addition.Owner, "properties", addition.Property, addition, "declaration", "added");
        foreach (var mapping in plan.Mappings)
        {
            var expression = Parse(mapping.Block, mapping.Value, mapping.Block.Node is FromSyntax);
            if (expression is null) return new(WorkspaceConflictKind.UnsupportedPropertySpan, [WorkspacePropertyAddPlan.Candidate(mapping.Target) with { Role = "mapping-target", Location = mapping.Block.Location }]);
            SyntaxNode node = mapping.Block.Node is FromSyntax ? new SetMappingSyntax(mapping.Target.Name, expression, mapping.Block.Location) : new PropertyMappingSyntax(mapping.Target.Name, expression, mapping.Block.Location);
            Insert(mapping.Block, "mappings", node, mapping.Target, "mapping-target", "added");
        }
        return request.Optional ? null : Fixtures();
    }

    internal ImmutableArray<WorkspaceOperation> Documents(ImmutableArray<WorkspacePropertyGeneration> generations)
    {
        foreach (var generation in generations.Where(generation => generation.Mode == "newGeneration"))
        {
            var owner = plan.Additions.First(addition => addition.Owner.Address!.Equals(generation.Event)).Owner;
            WorkspaceEventGenerations.Append(_roots[owner.Handle.Document], owner, generation.To);
        }
        return [.. _touched.OrderBy(document => document.ToString(), StringComparer.Ordinal).Select(document => new ReplaceWorkspaceSyntaxDocument(document, WorkspaceSyntaxMutation.Syntax(_roots[document])))];
    }

    static bool Fixture(SyntaxNode node) => node is SpecificationExampleSyntax or SpecificationCommandSyntax or SpecificationEventSyntax or SpecificationReadModelSyntax or SpecificationQueryResultSyntax;

    WorkspacePropertyRefusal? Fixtures()
    {
        var occurrences = index.Entries.Where(entry => Fixture(entry.Node)).SelectMany(entry => properties.Links.AdditionOwners(entry)
            .SelectMany(owner => plan.Additions.Where(addition => addition.AuthoredAddress == owner.Address).Select(addition => (Entry: entry, Addition: addition)))).ToArray();
        if (occurrences.Length > 0 && request.FixtureValue is null) return new(WorkspaceConflictKind.FixtureValueRequired, [.. occurrences.Select(occurrence => WorkspacePropertyAddPlan.Candidate(occurrence.Addition) with { Role = properties.Links.AdditionFixtureRole(occurrence.Entry), Location = occurrence.Entry.Location })]);
        foreach (var occurrence in occurrences)
        {
            var entry = occurrence.Entry;
            var value = Parse(entry, request.FixtureValue!, false, literal: true);
            if (value is null) return new(WorkspaceConflictKind.UnsupportedPropertySpan, [WorkspacePropertyAddPlan.Candidate(occurrence.Addition) with { Role = "fixture", Location = entry.Location }]);
            var member = entry.Node is SpecificationReadModelSyntax or SpecificationQueryResultSyntax ? "properties" : "values";
            Insert(entry, member, new PropertyMappingSyntax(occurrence.Addition.Name, value, entry.Location), occurrence.Addition, properties.Links.AdditionFixtureRole(entry), "fixtureAdded");
        }
        return null;
    }

    ExpressionSyntax? Parse(WorkspaceSyntaxEntry entry, string text, bool projection, bool literal = false)
    {
        var options = new[] { entry }.Concat(WorkspacePropertyPlan.Parents(entry, index)).Select(parent => parent.Node).OfType<ISourceSyntax>().FirstOrDefault()?.SourceOptions ?? SourceOptions.Legacy;
        var context = ParserContext.ForDiagnostics();
        context.SourceOptions = options;
        var expression = (literal, projection) switch
        {
            (true, _) => ExpressionParser.ParseLiteral(context, text, entry.Location),
            (_, true) => ExpressionParser.ParseProjectionExpression(context, text, entry.Location),
            _ => ExpressionParser.ParseMappingSource(context, text, entry.Location)
        };
        return expression is RawExpressionSyntax || context.Diagnostics.Count > 0 ? null : expression;
    }

    void Insert(WorkspaceSyntaxEntry entry, string member, SyntaxNode node, WorkspacePropertyAddition addition, string role, string change)
    {
        var target = WorkspaceSyntaxMutation.At(_roots[entry.Handle.Document], entry.Handle.Path);
        if (target[member] is not JsonArray collection) throw new InvalidWorkspaceAuthoring($"UnsupportedPropertySpan: {entry.Kind} has no {member} collection.");
        collection.Add(WorkspaceSyntaxMutation.Json(node));
        _touched.Add(entry.Handle.Document);
        _changes.Add(new(WorkspacePropertyAddPlan.Address(addition), addition.Kind, role, change, entry.Location));
    }
}
