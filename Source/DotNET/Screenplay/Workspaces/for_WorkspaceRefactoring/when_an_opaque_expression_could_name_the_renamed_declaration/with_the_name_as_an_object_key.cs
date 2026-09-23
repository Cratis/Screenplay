// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_an_opaque_expression_could_name_the_renamed_declaration;

public class with_the_name_as_an_object_key : given.a_workspace_with_a_structured_value
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith("""[{"Channel":"A-1","quantity":2}]""");

    void Because() => _result = RenameChannel();

    [Fact] void should_refuse_the_rename() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_identify_the_opaque_expression() => _result.Conflicts.Single().Message.Contains("RawExpressionSyntax", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_matched_name() => _result.Conflicts.Single().Message.Contains("names 'Channel'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_never_offer_partial_writes() => _result.WritePlan.ShouldBeNull();
}
