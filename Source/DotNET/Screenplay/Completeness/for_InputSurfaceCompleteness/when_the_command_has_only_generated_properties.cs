// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_the_command_has_only_generated_properties : given.a_model
{
    void Establish() => Compile("""
        concept CommandId : Uuid
        module M
          feature F
            slice StateChange Change
              command C
                id CommandId generated identifier
            slice StateView View
              screen S
                action C
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_not_require_typed_input() => Findings.ShouldBeEmpty();
}
