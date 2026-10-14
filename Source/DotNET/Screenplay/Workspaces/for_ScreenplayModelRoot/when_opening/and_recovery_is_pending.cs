// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening;

public class and_recovery_is_pending : given.a_persisted_model_root
{
    SortedDictionary<string, string> _before = null!;
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish()
    {
        File.WriteAllText(PendingPath, "pending");
        _before = Snapshot();
    }

    void Because() => _outcome = ScreenplayModelRoot.Open(RootPath);

    [Fact] void should_refuse() => _outcome.Success.ShouldBeFalse();
    [Fact] void should_name_the_pending_operation() => _outcome.FailureKind.ShouldEqual("PendingOperation");
    [Fact] void should_explain_recovery() => _outcome.Reason!.ShouldContain("recover-workspace");
    [Fact] void should_not_return_a_workspace() => _outcome.Workspace.ShouldBeNull();
    [Fact] void should_write_nothing() => Snapshot().ShouldEqual(_before);
}
