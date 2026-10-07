// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_only_guarded_choices_issue_commands : given.a_model
{
    void Establish() => Compile("""
        module M
          on load
            navigate to S
          feature F
            slice StateChange Change
              command C
                value String
              command Fallback
                value String
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

    [Fact] void should_accept_both_guarded_issuers() => Findings.ShouldBeEmpty();
}
