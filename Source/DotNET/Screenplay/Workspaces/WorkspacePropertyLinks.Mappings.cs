// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static bool ProductionStep(SyntaxNode node) => node is ProducesSyntax or CaptureAppendSyntax or InvokesSyntax;

    static bool MappingStep(SyntaxNode node) => node switch
    {
        CommandStreamSyntax => true,
        _ => ProductionStep(node) || FixtureStep(node)
    };

    static bool FixtureStep(SyntaxNode node) => node is SpecificationExampleSyntax or SpecificationCommandSyntax or SpecificationEventSyntax or SpecificationReadModelSyntax or SpecificationQueryResultSyntax or SpecificationQuerySyntax or SpecificationWhenQuerySyntax;

    static bool IncompleteMapping(PropertyMappingSyntax mapping, AuthoredDeclaration[] targets, PropertySource[] sources) =>
        targets.Length != 1 || mapping.Property.Contains('.') || sources.Any(item => item.Candidates.Length != 1 || item.Incomplete);

    static bool IncompleteMappingSource(PropertySource source, AuthoredDeclaration[] targets, bool ambiguous) => targets.Length != 1 || ambiguous || source.Incomplete;

    void IndexMappings()
    {
        foreach (var entry in _entries.Where(entry => entry.Node is PropertyMappingSyntax)) IndexMapping(entry);
    }

    void IndexMapping(WorkspaceSyntaxEntry entry)
    {
        var ancestors = Parents(entry).ToArray();
        if (ancestors.Any(ancestor => ancestor.Node is SpecificationStreamSyntax or SpecificationCaseSyntax)) return;
        var step = ancestors.FirstOrDefault(ancestor => MappingStep(ancestor.Node));
        if (step is null) return;
        if (step.Node is CommandStreamSyntax)
        {
            foreach (var source in ExpressionSources(((PropertyMappingSyntax)entry.Node).Source, entry))
            {
                Add(source.Entry, "key", new(source.Path, source.Candidates, Incomplete: source.Incomplete || source.Candidates.Length != 1));
            }
            return;
        }
        IndexMapping(entry, step);
    }

    void IndexMapping(WorkspaceSyntaxEntry entry, WorkspaceSyntaxEntry step)
    {
        var mapping = (PropertyMappingSyntax)entry.Node;
        var target = Members(FixtureOwner(step), mapping.Property);
        var production = ProductionStep(step.Node);
        var sources = production ? ExpressionSources(mapping.Source, entry).ToArray() : [];
        var source = sources.SelectMany(item => item.Candidates).Distinct().ToArray();
        var role = production ? "mapping-target" : FixtureRole(step);
        var ambiguousProduction = AmbiguousProduction(step);
        var incomplete = IncompleteMapping(mapping, target, sources) || ambiguousProduction || (production && mapping.Source is RawExpressionSyntax);
        Add(entry, role, new(mapping.Property, target, source is [var only] ? only : null, incomplete));
        foreach (var item in sources)
        {
            IndexMappingSource(item, target, ambiguousProduction);
        }
    }

    void IndexMappingSource(PropertySource source, AuthoredDeclaration[] targets, bool ambiguous)
    {
        var other = !ambiguous && targets is [var target] ? target : null;
        Add(source.Entry, "mapping-source", new(source.Path, source.Candidates, other, IncompleteMappingSource(source, targets, ambiguous)));
    }

    bool AmbiguousProduction(WorkspaceSyntaxEntry step) => step.Node is ProducesSyntax produced &&
        _index.ResolveProduction(produced.Event, Owner(step)?.Scope ?? []).Targets.Length != 1;
}
