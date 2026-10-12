// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static readonly string[] _assertionMembers = ["thenEvents", "thenReadModels", "thenQueries", "thenResults", "results"];

    static (string Name, string[] Kinds) FixtureReference(SyntaxNode node) => node switch
    {
        CaptureAppendSyntax value => (value.Event, ["Event"]),
        SeedEventSyntax value => (value.Event, ["Event"]),
        InvokesSyntax value => (value.Command, ["Command"]),
        SpecificationExampleSyntax value => (value.Type, ["Command", "Event", "ReadModel"]),
        SpecificationCommandSyntax value => (value.CommandType, ["Command", "Example"]),
        SpecificationEventSyntax value => (value.EventType, ["Event", "Example"]),
        SpecificationReadModelSyntax value => (value.Name, ["ReadModel", "Example"]),
        _ => (string.Empty, [])
    };

    static bool CaseStep(SyntaxNode node) => node is SpecificationCommandSyntax or SpecificationEventSyntax or SpecificationReadModelSyntax or SpecificationQueryResultSyntax or SpecificationQuerySyntax or SpecificationWhenQuerySyntax;

    void IndexCaseValues()
    {
        foreach (var entry in _entries.Where(entry => entry.Node is CaseValueExpressionSyntax)) IndexCaseValue(entry);
    }

    void IndexCaseValue(WorkspaceSyntaxEntry entry)
    {
        var parameter = ((CaseValueExpressionSyntax)entry.Node).Parameter;
        var ancestors = Parents(entry).ToArray();
        var mapping = ancestors.FirstOrDefault(parent => parent.Node is PropertyMappingSyntax);
        var step = ancestors.FirstOrDefault(parent => CaseStep(parent.Node));
        var table = ancestors.FirstOrDefault(parent => parent.Node is SpecificationSyntax);
        if ((mapping, step, table) is not ({ } owningMapping, { } owningStep, { } owningTable))
        {
            return;
        }

        var targets = Members(FixtureOwner(owningStep), ((PropertyMappingSyntax)owningMapping.Node).Property);
        var role = FixtureRole(owningStep);
        foreach (var row in CaseRows(parameter, owningTable)) Add(row, role, new(parameter, targets, Incomplete: targets.Length != 1));
    }

    IEnumerable<WorkspaceSyntaxEntry> CaseRows(string parameter, WorkspaceSyntaxEntry table) => _entries.Where(row =>
        row.Node is PropertyMappingSyntax value && value.Property == parameter &&
        Parents(row).Any(parent => parent.Node is SpecificationCaseSyntax) && Parents(row).Any(parent => parent.Handle == table.Handle));

    AuthoredDeclaration[] FixtureOwner(WorkspaceSyntaxEntry step)
    {
        if (step.Node is ProducesSyntax production)
        {
            return Current(_index.ResolveProduction(production.Event, Owner(step)?.Scope ?? []).Targets.Where(target => target.Kind == "Event"));
        }
        (string Name, string[] Kinds) reference = step.Node switch
        {
            SpecificationQueryResultSyntax => (QueryName(step), ["Query"]),
            SpecificationQuerySyntax value => (value.Query, ["Query"]),
            SpecificationWhenQuerySyntax value => (value.Query, ["Query"]),
            _ => FixtureReference(step.Node)
        };

        return FixtureType(Resolve(reference.Name, reference.Kinds, step), step);
    }

    AuthoredDeclaration[] FixtureType(AuthoredDeclaration[] targets, WorkspaceSyntaxEntry step)
    {
        if (targets is [var target] && target.Syntax is SpecificationExampleSyntax example) targets = ResolveAtOwner(example.Type, ["Command", "Event", "ReadModel"], target, step.Location);
        if (targets is [var query] && query.Syntax is QuerySyntax declared) targets = ResolveAtOwner(declared.ReturnType.Name, ["ReadModel"], query, step.Location);

        return targets;
    }

    string QueryName(WorkspaceSyntaxEntry entry)
    {
        var query = Parents(entry).Select(parent => parent.Node).OfType<SpecificationQuerySyntax>().FirstOrDefault();
        var specification = Parents(entry).Select(parent => parent.Node).OfType<SpecificationSyntax>().FirstOrDefault();

        return query?.Query ?? specification?.WhenQuery?.Query ?? string.Empty;
    }

    bool IsAssertion(WorkspaceSyntaxEntry step) => new[] { step }.Concat(Parents(step)).Any(entry => _assertionMembers.Contains(entry.Member, StringComparer.Ordinal));

    string FixtureRole(WorkspaceSyntaxEntry step) => step.Node is not (SpecificationQuerySyntax or SpecificationWhenQuerySyntax) && IsAssertion(step) ? "assertion" : "fixture";
}
