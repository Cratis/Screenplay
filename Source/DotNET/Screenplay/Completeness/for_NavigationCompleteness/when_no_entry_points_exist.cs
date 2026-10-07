// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_no_entry_points_exist : given.screens
{
    void Establish() => Compile("screen One\nscreen Two");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_report_once_for_the_application() => Findings.Single().Location.ShouldEqual(Compilation.Value!.Location);
    [Fact] void should_explain_that_all_screens_are_unreachable() => Findings.Single().Message.ShouldContain("none of its 2 screens is reachable");
}
