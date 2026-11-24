// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_navigation_item_in_a_reachable_screen_names_a_screen : given.screens
{
    void Establish() => Compile("screen Home\n  outlet sidebar extends\n    item detail title \"Detail\" target M.F screen Detail\nscreen Detail", "on load\n  navigate to Home", for_ScreenplayCompiler.when_compiling_a_navigation_screen_target.Layout);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_follow_the_navigation_edge() => Findings.ShouldBeEmpty();
}
