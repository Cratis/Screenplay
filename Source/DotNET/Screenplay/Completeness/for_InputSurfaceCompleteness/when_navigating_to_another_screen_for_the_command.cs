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

    [Fact] void should_report_only_the_target_action_without_its_own_surface() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ActionWithoutInputSurface);
    [Fact] void should_accept_the_originating_action() => Findings.Single().Location.Line.ShouldEqual(11);
}
