// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening_for_release_baseline_check;

public class with_an_unreadable_baseline : given.a_persisted_model_root
{
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish() => File.WriteAllText(BaselinePath, "<<<<<<< main");

    void Because() => _outcome = ScreenplayModelRoot.OpenForReleaseBaselineCheck(RootPath);

    [Fact] void should_succeed() => _outcome.Success.ShouldBeTrue();
    [Fact] void should_read_an_unreadable_baseline() => _outcome.ReleaseBaseline!.Status.ShouldEqual("unreadable");
    [Fact] void should_not_repair_the_file() => File.ReadAllText(BaselinePath).ShouldEqual("<<<<<<< main");
}
