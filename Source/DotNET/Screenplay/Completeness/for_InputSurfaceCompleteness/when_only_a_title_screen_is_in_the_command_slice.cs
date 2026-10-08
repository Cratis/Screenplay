// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_only_a_title_screen_is_in_the_command_slice : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice StateChange Change
              command C
                value String
              screen Input
                title "Input"
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_require_an_actual_command_issuer() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.CommandWithoutInputSurface);
}
