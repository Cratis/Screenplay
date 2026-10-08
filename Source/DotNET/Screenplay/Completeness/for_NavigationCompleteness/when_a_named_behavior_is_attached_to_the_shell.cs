// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_named_behavior_is_attached_to_the_shell : given.screens
{
    void Establish() => Compile("screen Home", "uses Go\n  target Home", "behavior Go\n  parameter target\n  on load\n    navigate to target\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_reach_the_instantiated_behavior_target() => Findings.ShouldBeEmpty();
}
