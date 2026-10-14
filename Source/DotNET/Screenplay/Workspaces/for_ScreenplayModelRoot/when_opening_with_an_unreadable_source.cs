// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot;

public class when_opening_with_an_unreadable_source : given.a_persisted_model_root
{
    ScreenplayModelRootOutcome[] _outcomes = [];
    Exception? _error;

    void Establish() => File.WriteAllBytes(Path.Combine(RootPath, "utf16.play"), Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("module M\n")).ToArray());

    void Because() => _error = Catch.Exception(() => _outcomes = [ScreenplayModelRoot.Open(RootPath), ScreenplayModelRoot.OpenForReleaseBaselineCheck(RootPath)]);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_refuse_both_opens() => _outcomes.All(outcome => !outcome.Success && outcome.Workspace is null).ShouldBeTrue();
    [Fact] void should_report_an_unreadable_model_root() => _outcomes.All(outcome => outcome.FailureKind == "UnreadableModelRoot").ShouldBeTrue();
}
