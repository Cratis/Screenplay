// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_an_unbound_container_id_collides : given.an_unbound_container_workspace
{
    ScreenplayWorkspace _collision = null!;
    SemanticIdentityAssignment _earlier = null!;
    SemanticAddress _slice = null!;
    WorkspaceContainerIdentities _report = null!;

    void Establish()
    {
        _slice = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice).Address;
        _earlier = new(SemanticAddress.ForModule(Application, "Sales"), SemanticId.Create(_slice), SemanticIdentityOrigin.Persisted);
    }

    void Because()
    {
        _collision = Create(Source, SemanticIdentityCatalog.Create(Application, [], [_earlier], []));
        _report = new(WorkspaceSyntaxIndex.Create(_collision));
    }

    [Fact] void should_keep_the_earlier_assignment() => _collision.IdentityCatalog.Semantics.ShouldContain(_earlier);
    [Fact] void should_leave_the_colliding_slice_unidentified() => _collision.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Equals(_slice)).ShouldBeFalse();
    [Fact] void should_report_the_collision() => _report.Issues.Single(issue => issue.Address.Equals(_slice)).Reason.ShouldEqual("idCollision");
    [Fact] void should_refuse_targeting_instead_of_substituting() => Catch.Exception(() => ModelNotesAnchors.RequireTarget(WorkspaceSyntaxIndex.Create(_collision), _slice)).Message.ShouldContain("idCollision");
}
