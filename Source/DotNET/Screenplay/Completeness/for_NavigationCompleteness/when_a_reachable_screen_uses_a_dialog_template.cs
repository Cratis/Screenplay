// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_reachable_screen_uses_a_dialog_template : given.screens
{
    void Establish() => Compile("screen Home\n  on click\n    open dialog Editor\nscreen Input\n  template Editor\n    body\n      title \"Input\"\nscreen Detail", "on load\n  navigate to Home\ndialog template Editor\n  body\n  on load\n    navigate to Detail");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_follow_the_opened_dialog_behavior() => Findings.ShouldBeEmpty();
}
