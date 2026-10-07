// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_template_behavior_enters_a_screen : given.screens
{
    void Establish() => Compile("screen Home", "screen template Page\n  body\n  on load\n    navigate to Home");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_not_make_an_unused_template_an_entry_point() => Findings.Length.ShouldEqual(1);
    [Fact] void should_report_no_entry_points() => Findings.Single().Message.ShouldContain("no navigation entry points");
}
