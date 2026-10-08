// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_named_behavior_is_attached_to_a_screen : given.screens
{
    void Establish() => Compile("screen Home\n  uses Go\n    target Detail\nscreen Detail", "on load\n  navigate to Home", "behavior Go\n  parameter target\n  on load\n    navigate to target\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_resolve_the_literal_argument_at_its_uses_site() => Findings.ShouldBeEmpty();
}
