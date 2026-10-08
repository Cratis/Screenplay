// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_an_attached_behavior_opens_an_issuing_dialog : given.a_model
{
    void Establish() => Compile("""
        module M
          dialog template Editor
            body
          on load
            navigate to Home
          feature F
            slice StateChange Change
              command C
                value String
              screen Input
                template Editor
                  body
                    action C
            slice StateView View
              screen Home
                on click
                  open dialog Editor
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_accept_the_reached_issuing_screen() => Findings.ShouldBeEmpty();
}
