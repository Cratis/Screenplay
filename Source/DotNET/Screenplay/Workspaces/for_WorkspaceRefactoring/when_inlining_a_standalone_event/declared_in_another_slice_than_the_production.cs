// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_inlining_a_standalone_event;

public class declared_in_another_slice_than_the_production : given.a_standalone_event
{
    protected override string Source => Eligible.Replace("    slice StateChange Rename\n      event Renamed\n        id \"OldName\" // identity intent\n        description \"A new name\" // description intent\n        documentation\n          ```markdown\n          Name history.\n          ```\n        tag audit\n        name String\n", "    slice StateView Elsewhere\n      event Renamed\n        id \"OldName\" // identity intent\n        description \"A new name\" // description intent\n        documentation\n          ```markdown\n          Name history.\n          ```\n        tag audit\n        name String\n    slice StateChange Rename\n", StringComparison.Ordinal);

    void Because() => _result = Inline();

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_why() => _result.Conflicts.Single().Message.ShouldContain("same slice as the event");
    [Fact] void should_leave_the_source_unchanged() => (_result.Workspace is null && _workspace.Documents[0].Text == Source).ShouldBeTrue();
}
