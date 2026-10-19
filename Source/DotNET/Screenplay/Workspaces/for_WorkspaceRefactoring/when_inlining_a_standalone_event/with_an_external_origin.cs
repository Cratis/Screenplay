// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_inlining_a_standalone_event;

public class with_an_external_origin : given.a_standalone_event
{
    protected override string Source => Eligible.Replace("event Renamed", "event Renamed from \"Elsewhere\" // event intent", StringComparison.Ordinal);

    void Because() => _result = Inline();

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_why() => _result.Conflicts.Single().Message.ShouldContain("external-origin or public");
    [Fact] void should_leave_the_source_unchanged() => (_result.Workspace is null && _workspace.Documents[0].Text == Source).ShouldBeTrue();
}
