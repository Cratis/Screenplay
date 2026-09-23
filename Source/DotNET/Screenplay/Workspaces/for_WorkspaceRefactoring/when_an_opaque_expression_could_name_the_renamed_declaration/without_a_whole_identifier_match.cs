// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_an_opaque_expression_could_name_the_renamed_declaration;

public class without_a_whole_identifier_match : given.a_workspace_with_a_structured_value
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith("[{\"sku\":\"ChannelSuffix\",\"quantity\":2}]");

    void Because() => _result = RenameChannel();

    [Fact] void should_accept_the_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_keep_the_longer_identifier() => _result.Workspace!.Documents.Single(document => document.Id == Order.Id).Text.Contains("\"sku\":\"ChannelSuffix\"", StringComparison.Ordinal).ShouldBeTrue();
}
