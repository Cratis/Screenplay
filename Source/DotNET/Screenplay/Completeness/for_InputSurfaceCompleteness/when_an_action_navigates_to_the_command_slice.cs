// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_an_action_navigates_to_the_command_slice : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice StateChange Change
              command C
                value String
              screen Input
                title "Input"
            slice StateView View
              screen S
                action C
                  navigate to Input
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_not_treat_a_title_only_destination_as_input() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ActionWithoutInputSurface);
}
