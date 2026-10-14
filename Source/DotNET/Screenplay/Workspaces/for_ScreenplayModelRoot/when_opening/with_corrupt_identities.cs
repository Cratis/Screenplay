// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening;

public class with_corrupt_identities : given.a_persisted_model_root
{
    SortedDictionary<string, string> _before = null!;
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish()
    {
        File.WriteAllText(IdentitiesPath, "<<<<<<< main");
        _before = Snapshot();
    }

    void Because() => _outcome = ScreenplayModelRoot.Open(RootPath);

    [Fact] void should_refuse() => _outcome.Success.ShouldBeFalse();
    [Fact] void should_report_an_identity_state_conflict() => _outcome.FailureKind.ShouldEqual("IdentityStateConflict");
    [Fact] void should_name_the_identity_state_conflict() => _outcome.Reason!.ShouldContain("IdentityStateConflict:");
    [Fact] void should_not_return_a_workspace() => _outcome.Workspace.ShouldBeNull();
    [Fact] void should_not_repair_the_identities() => File.ReadAllText(IdentitiesPath).ShouldEqual("<<<<<<< main");
    [Fact] void should_write_nothing() => Snapshot().ShouldEqual(_before);
}
