// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_naming_the_opaque_conflict_location : given.a_workspace_with_a_structured_value
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith("[{\"sku\":\"Channel\",\"quantity\":2}]");

    void Because() => _result = RenameChannel();

    [Fact] void should_name_the_document_path_and_line() => _result.Conflicts.Single().Message.Contains("'Shop/Orders/PlaceOrder/PlaceOrder.play(22,", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_syntax_pointer() => _result.Conflicts.Single().Message.Contains("/specifications/0/when/values/2/source", StringComparison.Ordinal).ShouldBeTrue();
}
