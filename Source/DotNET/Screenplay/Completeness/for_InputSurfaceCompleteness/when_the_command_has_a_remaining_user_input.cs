// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_the_command_has_a_remaining_user_input : given.a_model
{
    void Establish() => Compile("""
        concept CommandId : Uuid
        module M
          feature F
            slice StateChange Change
              command C
                id CommandId generated identifier
                value String
            slice StateView View
              screen S
                action C
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_require_a_surface_for_the_non_generated_property() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ActionWithoutInputSurface);
}
