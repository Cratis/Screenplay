// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_beside_an_unrelated_structured_value : given.a_workspace_with_a_structured_value
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = RenameChannel();

    [Fact] void should_accept_the_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_repair_the_command_and_event_references() => _result.Workspace!.Documents.Single(document => document.Id == Order.Id).Text.Split("channel SalesChannel").Length.ShouldEqual(3);
    [Fact] void should_leave_the_structured_value_untouched() => _result.Workspace!.Documents.Single(document => document.Id == Order.Id).Text.Contains($"lines = {StructuredValue}", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_comment() => _result.Workspace!.Documents.Single(document => document.Id == Order.Id).Text.Contains("// @public command PlaceOrder", StringComparison.Ordinal).ShouldBeTrue();
}
