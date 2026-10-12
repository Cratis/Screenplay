// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_of_an_example_value_and_an_unrelated_authoring_error : given.a_projection_effect_workspace
{
    string _source;
    string _catalogRevision;

    void Establish()
    {
        _source = ExampleSource;
        Workspace = ScreenplayWorkspace.Create("Projects",
            [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(_source)),
             WorkspaceDocument.Create("unrelated", PortablePlayPath.Parse("unrelated.play"), Encoding.UTF8.GetBytes("concept Unknown MissingType"))],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _catalogRevision = Workspace.IdentityCatalog.Revision.ToString();
    }

    void Because() => Result = Workspace.ProposeProperty(Request("Audited", "remove") with { EventNeverPersisted = true });

    [Fact] void should_have_authoring_compilation_errors() => new AuthoringSnapshot(Workspace.Documents).Compilation.Success.ShouldBeFalse();
    [Fact] void should_retain_the_authoring_tree_despite_errors() => new AuthoringSnapshot(Workspace.Documents).Compilation.Value.ShouldNotBeNull();
    [Fact] void should_refuse_the_invalid_candidate() => Result.Conflicts.Select(conflict => conflict.Kind).ShouldContainOnly(WorkspaceConflictKind.CompilationFailed);
    [Fact] void should_not_return_a_candidate_workspace() => Result.Workspace.ShouldBeNull();
    [Fact] void should_not_return_a_write_plan() => Result.WritePlan.ShouldBeNull();
    [Fact] void should_not_report_successful_changes() => Result.PropertyReport.Changed.ShouldBeEmpty();
    [Fact] void should_not_report_event_evolutions() => Result.PropertyReport.Generations.ShouldBeEmpty();
    [Fact] void should_preserve_source() => Workspace.Documents.Single(document => document.Path.Value == "model.play").Text.ShouldEqual(_source);
    [Fact] void should_preserve_catalog() => Workspace.IdentityCatalog.Revision.ToString().ShouldEqual(_catalogRevision);
}
