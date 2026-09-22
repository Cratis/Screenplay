// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_moving_a_node_between_documents : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    WorkspaceSyntaxEntry _concept = null!;

    void Establish() => _concept = Index.Entries.First(entry => entry.Node is ConceptSyntax);

    void Because() => _result = Workspace.ProposeAuthoring(Authoring(new MoveWorkspaceNode(
        _concept.Handle, _concept.Node, RegistrationRoot.Handle, RegistrationRoot.Node, "concepts")));

    [Fact] void should_accept_the_final_cross_file_state() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_write_both_documents() => _result.WritePlan.Entries.Length.ShouldEqual(2);
    [Fact] void should_keep_the_logical_identity() => _result.Workspace!.IdentityCatalog.ResolveSemantic(_concept.Address!).ShouldEqual(_concept.SemanticId!.Value);
    [Fact] void should_move_the_occurrence_owner() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Single(entry => entry.Address?.Equals(_concept.Address) == true).Handle.Document.ShouldEqual(Registration.Id);
    [Fact] void should_keep_the_original_snapshot() => WorkspaceSyntaxIndex.Create(Workspace).Find(_concept.Handle).ShouldNotBeNull();
}
