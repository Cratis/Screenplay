// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertyAssertionProvenance(WorkspaceSyntaxIndex index)
{
    readonly Lazy<EffectiveSpecificationApplication?> _expanded = new(() => new AuthoringSnapshot(index.Workspace.Documents).Compilation.Value is { } application ? SpecificationExamples.Expand(application) : null);

    internal IEnumerable<SourceLocation> Consumers(WorkspaceSyntaxEntry occurrence)
    {
        if (occurrence.Node is not PropertyMappingSyntax mapping) return [];
        var example = WorkspacePropertyPlan.Parents(occurrence, index).Select(entry => entry.Node).OfType<SpecificationExampleSyntax>().FirstOrDefault();
        if (example is null || _expanded.Value is not { } expanded) return [];
        return expanded.Specifications.SelectMany(specification => specification.Steps)
            .Where(step => (step.Role == "then" || step.Role == "then readmodel") && SameExample(step.Example, example) && Inherits(step, mapping.Property))
            .Select(step => step.Authored.Location).Distinct();
    }

    static bool SameExample(SpecificationExampleSyntax? resolved, SpecificationExampleSyntax authored) => resolved is not null && resolved.Name == authored.Name && resolved.Location == authored.Location;

    static bool Inherits(EffectiveSpecificationStep step, string property) => step.Values.Any(value => value.Property == property && value.Origin is SpecificationValueOrigin.Example or SpecificationValueOrigin.Case) &&
        !AuthoredValues(step.Authored).Any(value => value.Property == property);

    static IEnumerable<PropertyMappingSyntax> AuthoredValues(SyntaxNode step) => step switch
    {
        SpecificationEventSyntax @event => @event.Values,
        SpecificationReadModelSyntax model => model.Properties,
        _ => []
    };
}
