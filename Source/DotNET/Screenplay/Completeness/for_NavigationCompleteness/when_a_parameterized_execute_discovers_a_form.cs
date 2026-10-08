// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_parameterized_execute_discovers_a_form : given.screens
{
    void Establish() => Compile("command Go\nscreen Home\n  uses Submit\n    command Go\nscreen Detail", "on load\n  navigate to Home\nform Input for Go\n  on submit navigate to Detail", "behavior Submit\n  parameter command\n  on click\n    execute command\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_resolve_the_command_argument_at_the_attachment() => Findings.ShouldBeEmpty();
}
