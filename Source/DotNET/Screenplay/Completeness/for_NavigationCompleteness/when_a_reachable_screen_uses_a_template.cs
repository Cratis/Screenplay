// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_reachable_screen_uses_a_template : given.screens
{
    void Establish() => Compile("screen Home\n  template Page\n    body\n      title \"Home\"\nscreen Detail", "on load\n  navigate to Home\nscreen template Page\n  body\n  on load\n    navigate to Detail");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_follow_the_used_template_behavior() => Findings.ShouldBeEmpty();
}
