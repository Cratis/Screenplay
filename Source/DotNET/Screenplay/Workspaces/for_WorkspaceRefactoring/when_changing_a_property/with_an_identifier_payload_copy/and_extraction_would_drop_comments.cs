// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_extraction_would_drop_comments : given.an_identifier_payload_copy_workspace
{
    void Establish() => Create(InlineSource.Replace("name String = \"something\"", "name String = \"something\" // why this matters", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_refuse() => Result.Accepted.ShouldBeFalse();
    [Fact] void should_not_plan_a_write() => Result.WritePlan.ShouldBeNull();
    [Fact] void should_not_retain_a_candidate() => Result.Workspace.ShouldBeNull();
}
