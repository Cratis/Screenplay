// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_in_an_unindexable_example_document : given.a_projection_effect_workspace
{
    string _source;
    string _catalogRevision;
    WorkspacePropertyRequest _request;

    void Establish()
    {
        _source = ExampleSource.Replace("example Expected : Audited\n        name = \"Ada\"", "example Expected : Audited\n        name = \"Ada\"\n        name = \"Grace\"", StringComparison.Ordinal);
        UseBindableSource(ExampleSource);
        _request = Request("Audited", "remove") with { EventNeverPersisted = true };
        Workspace = Create(_source);
        _request = _request with
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = _request.Target with { Revision = Workspace.Revision }
        };
        _catalogRevision = Workspace.IdentityCatalog.Revision.ToString();
    }

    void Because() => Result = Workspace.ProposeProperty(_request);

    [Fact] void should_have_authoring_compilation_errors() => new AuthoringSnapshot(Workspace.Documents).Compilation.Success.ShouldBeFalse();
    [Fact] void should_retain_the_authoring_tree_despite_errors() => new AuthoringSnapshot(Workspace.Documents).Compilation.Value.ShouldNotBeNull();
    [Fact] void should_not_index_the_erroneous_document_as_editable() => WorkspaceSyntaxIndex.Create(Workspace).Entries.ShouldBeEmpty();
    [Fact] void should_refuse_the_unindexable_target() => Result.Conflicts.Select(conflict => conflict.Kind).ShouldContainOnly(WorkspaceConflictKind.UnsupportedPropertySpan);
    [Fact] void should_not_return_a_candidate_workspace() => Result.Workspace.ShouldBeNull();
    [Fact] void should_not_return_a_write_plan() => Result.WritePlan.ShouldBeNull();
    [Fact] void should_not_report_successful_changes() => Result.PropertyReport.Changed.ShouldBeEmpty();
    [Fact] void should_not_report_event_evolutions() => Result.PropertyReport.Generations.ShouldBeEmpty();
    [Fact] void should_preserve_source() => Workspace.Documents.Single().Text.ShouldEqual(_source);
    [Fact] void should_preserve_catalog() => Workspace.IdentityCatalog.Revision.ToString().ShouldEqual(_catalogRevision);
}
