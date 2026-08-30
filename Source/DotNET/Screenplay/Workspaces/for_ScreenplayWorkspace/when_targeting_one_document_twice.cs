// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_targeting_one_document_twice : given.a_valid_workspace
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(
        new MoveWorkspaceDocument
        {
            Document = Concepts.Id,
            Path = PortablePlayPath.Parse("Common/application.play")
        },
        new ReplaceWorkspaceDocument
        {
            Document = Concepts.Id,
            Bytes = Bytes(ConceptsSource)
        }));

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_an_invalid_operation() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
}
