// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_an_inline_behavior_executes_the_command : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice StateChange Change
              command C
                value String
              screen S
                on click
                  confirm "Proceed?"
                    on success
                      execute C
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_include_continuation_issuers() => Findings.ShouldBeEmpty();
}
