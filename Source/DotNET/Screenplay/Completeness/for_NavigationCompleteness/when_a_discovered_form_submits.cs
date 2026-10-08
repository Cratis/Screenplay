// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_discovered_form_submits : given.screens
{
    void Establish() => Compile("command Go\nscreen Home\n  action Go\nscreen Detail", "on load\n  navigate to Home\nform Input for Go\n  on submit navigate to Detail");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_follow_the_discovered_forms_submit_edge() => Findings.ShouldBeEmpty();
}
