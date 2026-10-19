// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_inlining_a_standalone_event;

public class with_an_opaque_handler_naming_the_event : given.a_standalone_event
{
    protected override string Source => Eligible.Replace("      command Rename\n", "      command Opaque\n        handler\n          ```csharp\n          yield return new Renamed(name);\n          ```\n      command Rename\n", StringComparison.Ordinal);

    void Because() => _result = Inline();

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_why() => _result.Conflicts.Single().Message.ShouldContain("opaque code");
    [Fact] void should_leave_the_source_unchanged() => (_result.Workspace is null && _workspace.Documents[0].Text == Source).ShouldBeTrue();
}
