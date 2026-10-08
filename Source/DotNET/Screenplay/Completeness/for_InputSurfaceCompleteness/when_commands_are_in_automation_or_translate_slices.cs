// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_commands_are_in_automation_or_translate_slices : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice Automation Automatic
              command C
                value String
            slice Translate Translation
              command D
                value String
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_not_require_person_invocation() => Findings.ShouldBeEmpty();
}
