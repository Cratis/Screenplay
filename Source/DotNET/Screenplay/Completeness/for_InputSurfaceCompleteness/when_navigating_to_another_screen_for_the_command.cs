// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_navigating_to_another_screen_for_the_command : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice StateChange Change
              command C
                value String
            slice StateView View
              screen S
                action C
                  navigate to Input
              screen Input
                action C
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_report_both_actions_without_their_own_surfaces() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ActionWithoutInputSurface, DiagnosticCodes.ActionWithoutInputSurface);
    [Fact] void should_not_use_post_action_navigation_as_input() => Findings.Select(finding => finding.Location.Line).ShouldContainOnly(8, 11);
}
