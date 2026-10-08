// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_the_action_is_in_the_command_slice : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice StateChange Change
              command C
                value String
              screen S
                action C
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_accept_the_input_screen() => Findings.ShouldBeEmpty();
}
