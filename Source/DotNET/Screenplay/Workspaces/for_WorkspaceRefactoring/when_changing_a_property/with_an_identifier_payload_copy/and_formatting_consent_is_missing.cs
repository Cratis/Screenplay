// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_formatting_consent_is_missing : given.an_identifier_payload_copy_workspace
{
    void Establish() => Create(InlineSource);

    void Because() => Result = Workspace.ProposeProperty(Request() with { Formatting = WorkspaceAuthoringFormatting.PreserveTrivia });

    [Fact] void should_require_consent() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);
    [Fact] void should_not_plan_a_write() => Result.WritePlan.ShouldBeNull();
}
