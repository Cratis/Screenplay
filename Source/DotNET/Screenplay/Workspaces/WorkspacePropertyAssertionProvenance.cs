// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertyAssertionProvenance(
    WorkspaceSyntaxIndex index,
    Lazy<EffectiveSpecificationApplication?> expanded,
    Lazy<IEnumerable<Diagnostic>>? compilationDiagnostics = null)
{
    internal WorkspacePropertyAssertionProvenance(WorkspaceSyntaxIndex index)
        : this(index, new AuthoringSnapshot(index.Workspace.Documents))
    {
    }

    WorkspacePropertyAssertionProvenance(WorkspaceSyntaxIndex index, AuthoringSnapshot snapshot)
        : this(index, new(() => snapshot.Compilation.Value is { } application ? SpecificationExamples.Expand(application) : null), new(() => snapshot.Compilation.Diagnostics))
    {
    }

    // Null means example provenance is unavailable, not that no assertion consumes the value.
    internal IEnumerable<SourceLocation>? Consumers(WorkspaceSyntaxEntry occurrence)
    {
        if (occurrence.Node is not PropertyMappingSyntax mapping) return [];
        var example = WorkspacePropertyPlan.Parents(occurrence, index).FirstOrDefault(entry => entry.Node is SpecificationExampleSyntax);
        if (example is null) return [];
        if (expanded.Value is not { } application) return null;
        if (RelevantErrors(example, application.Diagnostics.Concat(compilationDiagnostics?.Value ?? []))) return null;
        return ThenConsumers(application, (SpecificationExampleSyntax)example.Node, mapping.Property);
    }

    static IEnumerable<SourceLocation> ThenConsumers(EffectiveSpecificationApplication application, SpecificationExampleSyntax example, string property) =>
        application.Specifications.SelectMany(specification => specification.Steps)
            .Where(step => Then(step) && SameExample(step.Example, example) && Inherits(step, property))
            .Select(step => step.Authored.Location).Distinct();

    static bool Then(EffectiveSpecificationStep step) => step.Role is "then" or "then readmodel";

    static string? ExampleName(SyntaxNode node) => node switch
    {
        SpecificationEventSyntax @event => @event.EventType,
        SpecificationCommandSyntax command => command.CommandType,
        SpecificationReadModelSyntax model => model.Name,
        _ => null
    };

    static bool SameExample(SpecificationExampleSyntax? resolved, SpecificationExampleSyntax authored) => resolved is not null && resolved.Name == authored.Name && resolved.Location == authored.Location;

    static bool Inherits(EffectiveSpecificationStep step, string property) => step.Values.Any(value => value.Property == property && value.Origin is SpecificationValueOrigin.Example or SpecificationValueOrigin.Case) &&
        !AuthoredValues(step.Authored).Any(value => value.Property == property);

    static IEnumerable<PropertyMappingSyntax> AuthoredValues(SyntaxNode step) => step switch
    {
        SpecificationEventSyntax @event => @event.Values,
        SpecificationReadModelSyntax model => model.Properties,
        _ => []
    };

    bool RelevantErrors(WorkspaceSyntaxEntry example, IEnumerable<Diagnostic> diagnostics)
    {
        var name = ((SpecificationExampleSyntax)example.Node).Name;
        var specifications = index.Entries.Where(entry => entry.Node is SpecificationSyntax && UsesExample(entry, name));
        var regions = specifications.Prepend(example).ToArray();
        return diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && regions.Any(region => Contains(region, diagnostic.Location)));
    }

    bool UsesExample(WorkspaceSyntaxEntry specification, string name) => index.Entries.Any(entry =>
        ExampleName(entry.Node)?.Split('.')[^1] == name && WorkspacePropertyPlan.Parents(entry, index).Any(parent => parent.Handle == specification.Handle));

    bool Contains(WorkspaceSyntaxEntry region, SourceLocation location)
    {
        if (location.Path != region.Location.Path || location.Line < region.Location.Line) return false;
        var document = index.Workspace.Documents.Single(document => document.Id == region.Handle.Document);
        var lines = SourceLineSplitter.Split(document.Text, path: document.Path.Value);
        var header = lines.Single(line => line.Number == region.Location.Line);
        var end = lines.FirstOrDefault(line => line.Number > header.Number && !line.IsBlank && line.Indent <= header.Indent)?.Number ?? int.MaxValue;
        return location.Line < end;
    }
}
