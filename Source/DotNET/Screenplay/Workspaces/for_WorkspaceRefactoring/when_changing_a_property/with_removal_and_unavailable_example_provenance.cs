// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_and_unavailable_example_provenance : given.a_projection_effect_workspace
{
    WorkspacePropertyAssertionProvenance _provenance;
    WorkspacePropertyCandidate _candidate;
    string _source;
    string _catalogRevision;

    void Establish()
    {
        UseBindableSource(ExampleSource);
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var occurrence = index.Entries.Single(entry => entry.Node is PropertyMappingSyntax mapping && mapping.Property == "name" && WorkspacePropertyPlan.Parents(entry, index).Any(parent => parent.Node is SpecificationExampleSyntax));
        _candidate = new(Property("Audited").Address!, "Event", "fixture", occurrence.Location);
        _provenance = new(index, new(() => null));
        _source = Workspace.Documents.Single().Text;
        _catalogRevision = Workspace.IdentityCatalog.Revision.ToString();
    }

    void Because() => Result = new WorkspaceRefactoring(Workspace).Property(Request("Audited", "remove") with { EventNeverPersisted = true }, _provenance);

    [Fact] void should_refuse_unavailable_assertion_provenance() => Result.Conflicts.Select(conflict => conflict.Kind).ShouldContainOnly(WorkspaceConflictKind.AmbiguousPropertyReference);
    [Fact] void should_identify_the_removed_example_occurrence() => Result.Conflicts.SelectMany(conflict => conflict.Candidates).ShouldContainOnly(_candidate);
    [Fact] void should_not_accept_the_proposal() => Result.Accepted.ShouldBeFalse();
    [Fact] void should_not_return_a_workspace() => Result.Workspace.ShouldBeNull();
    [Fact] void should_not_return_a_write_plan() => Result.WritePlan.ShouldBeNull();
    [Fact] void should_not_report_changes() => Result.PropertyReport.Changed.ShouldBeEmpty();
    [Fact] void should_not_report_generations() => Result.PropertyReport.Generations.ShouldBeEmpty();
    [Fact] void should_preserve_source() => Workspace.Documents.Single().Text.ShouldEqual(_source);
    [Fact] void should_preserve_catalog() => Workspace.IdentityCatalog.Revision.ToString().ShouldEqual(_catalogRevision);
}
