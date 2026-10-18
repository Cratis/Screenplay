// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_unbound_containers_are_ambiguous : given.an_unbound_container_workspace
{
    ScreenplayWorkspace _duplicate = null!;
    ScreenplayWorkspace _identified = null!;
    SemanticAddress _slice = null!;
    WorkspaceContainerIdentities _report = null!;

    void Because()
    {
        const string source = Source + "\n      slice StateChange PlaceOrder\n        event OtherFact\n";
        _duplicate = Create(source);
        _identified = Create(source, Workspace.IdentityCatalog);
        _slice = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice).Address;
        _report = new(WorkspaceSyntaxIndex.Create(_duplicate));
    }

    [Fact] void should_not_assign_a_duplicate_slice() => _duplicate.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Equals(_slice)).ShouldBeFalse();
    [Fact] void should_report_the_reason() => _report.Issues.Single(issue => issue.Address.Equals(_slice)).Reason.ShouldEqual("ambiguous");
    [Fact] void should_keep_an_existing_assignment() => _identified.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
    [Fact] void should_flag_the_assigned_duplicate() => new WorkspaceContainerIdentities(WorkspaceSyntaxIndex.Create(_identified)).Issues.Single().Assigned.ShouldBeTrue();
    [Fact] void should_refuse_new_targeting() => Catch.Exception(() => ModelNotesAnchors.RequireTarget(WorkspaceSyntaxIndex.Create(_identified), _slice)).ShouldBeOfExactType<InvalidModelNotes>();
}
