// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_a_reaction_invokes_the_command : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice StateChange Change
              command C
                value String
              event Changed
                value String
            slice Automation FollowUp
              reaction React
                when Changed
                  invokes C
                    value = value
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_exempt_the_automated_invocation() => Findings.ShouldBeEmpty();
}
