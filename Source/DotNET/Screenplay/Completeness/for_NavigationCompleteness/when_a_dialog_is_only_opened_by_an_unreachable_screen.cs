// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_dialog_is_only_opened_by_an_unreachable_screen : given.screens
{
    void Establish() => Compile("screen Home\nscreen Lost\n  on click\n    open dialog Editor\nscreen Detail\n  template Editor\n    body\n      title \"Details\"", "on load\n  navigate to Home\ndialog template Editor\n  body");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_not_make_a_dialog_screen_a_root() => Findings.Length.ShouldEqual(2);
}
