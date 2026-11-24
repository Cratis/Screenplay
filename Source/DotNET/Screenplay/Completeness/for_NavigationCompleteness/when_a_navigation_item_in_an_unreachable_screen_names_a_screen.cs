// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_navigation_item_in_an_unreachable_screen_names_a_screen : given.screens
{
    void Establish() => Compile("screen Hidden\n  outlet sidebar extends\n    item hidden title \"Hidden\" target M.F screen Hidden", root: for_ScreenplayCompiler.when_compiling_a_navigation_screen_target.Layout);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_not_create_a_navigation_entry_point() => Findings.Single().Location.Line.ShouldEqual(0);
    [Fact] void should_report_that_the_screen_is_unreachable() => Findings.Single().Message.ShouldContain("none of its 1 screens is reachable");
}
