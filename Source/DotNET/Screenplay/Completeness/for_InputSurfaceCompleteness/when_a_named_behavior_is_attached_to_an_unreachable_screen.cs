// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_a_named_behavior_is_attached_to_an_unreachable_screen : given.a_model
{
    void Establish() => Compile("""
        behavior Reusable
          on click
            execute M.F.Change.C
        module M
          on load
            navigate to Home
          feature F
            slice StateChange Change
              command C
                value String
              screen Lost
                uses Reusable
            slice StateView View
              screen Home
                title "Home"
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_not_count_the_disconnected_attachment_as_an_issuer() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.CommandWithoutInputSurface);
}
