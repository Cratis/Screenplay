// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_contribution_enters_a_screen : given.screens
{
    void Establish() => Compile("screen Home", "contribute to Navigation\n  navigate to Home", "layout Shell\n  nav contributes Navigation\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_reach_the_contributed_entry_screen() => Findings.ShouldBeEmpty();
}
