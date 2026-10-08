// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_an_inline_behavior_navigates : given.screens
{
    void Establish() => Compile("screen Home\n  on load\n    navigate to Detail\nscreen Detail", "on load\n  navigate to Home");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_follow_the_attached_behavior_edge() => Findings.ShouldBeEmpty();
}
