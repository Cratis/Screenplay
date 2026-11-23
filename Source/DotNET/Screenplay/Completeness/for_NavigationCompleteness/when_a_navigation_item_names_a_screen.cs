// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_navigation_item_names_a_screen : given.screens
{
    void Establish() => Compile("screen CreateProject\nscreen Projects", "outlet sidebar extends\n  item projects title \"Projects\" target M.F screen Projects", for_ScreenplayCompiler.when_compiling_a_navigation_screen_target.Layout);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_root_only_the_selected_screen() => Findings.Length.ShouldEqual(1);
    [Fact] void should_leave_the_form_screen_unreachable() => Findings.Single().Message.ShouldContain("CreateProject");
}
