// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_disconnected_navigation_item_names_a_screen : given.screens
{
    void Establish() => Compile("screen Home\nscreen Hidden\n  outlet sidebar extends\n    item detail title \"Detail\" target M.F screen Detail\nscreen Detail", "on load\n  navigate to Home", for_ScreenplayCompiler.when_compiling_a_navigation_screen_target.Layout);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_leave_the_containing_screen_unreachable() => Findings.ShouldContain(finding => finding.Message.Contains("Hidden"));
    [Fact] void should_leave_the_target_screen_unreachable() => Findings.ShouldContain(finding => finding.Message.Contains("Detail"));
    [Fact] void should_report_both_disconnected_screens() => Findings.Length.ShouldEqual(2);
}
