// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_a_command_bound_form_exists : given.a_model
{
    void Establish() => Compile("""
        module M
          form Input for C
            field value
          feature F
            slice StateChange Change
              command C
                value String
            slice StateView View
              screen S
                action C
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_accept_the_discovered_form() => Findings.ShouldBeEmpty();
}
