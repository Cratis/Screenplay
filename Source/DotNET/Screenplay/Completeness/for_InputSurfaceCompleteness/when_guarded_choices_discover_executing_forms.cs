// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_guarded_choices_discover_executing_forms : given.a_model
{
    void Establish() => Compile("""
        module M
          on load
            navigate to S
          form Input for C
            on submit
              execute FromChoice
          form FallbackInput for Fallback
            on submit
              execute FromFallback
          feature F
            slice StateChange Change
              command C
              command Fallback
              command FromChoice
              command FromFallback
              readmodel Item
                value String
              query Get => Item
              screen S
                data Item via query Get
                action "Choose"
                  when item.value == "ready" execute C
                  otherwise execute Fallback
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_discover_executions_from_both_forms() => Findings.ShouldBeEmpty();
}
