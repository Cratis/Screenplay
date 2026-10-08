// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_a_behavior_executes_the_command : given.a_model
{
    void Establish() => Compile("""
        behavior Execute
          parameter command
          on click
            execute command
        module M
          feature F
            slice StateChange Change
              command C
                value String
              screen S
                uses Execute
                  command C
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_resolve_the_literal_behavior_argument() => Findings.ShouldBeEmpty();
}
