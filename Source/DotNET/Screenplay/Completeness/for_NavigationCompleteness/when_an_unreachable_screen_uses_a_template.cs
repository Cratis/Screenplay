// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_an_unreachable_screen_uses_a_template : given.screens
{
    void Establish() => Compile("screen Home\nscreen Lost\n  template Page\n    body\n      title \"Lost\"\nscreen Detail", "on load\n  navigate to Home\nscreen template Page\n  body\n  on load\n    navigate to Detail");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_leave_the_user_and_target_unreachable() => Findings.Select(finding => finding.Message).ShouldContainOnly("Screen 'Lost' is unreachable from navigation entry points", "Screen 'Detail' is unreachable from navigation entry points");
}
