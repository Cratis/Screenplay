// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_a_form_is_the_only_ui_issuer : given.a_model
{
    void Establish() => Compile("""
        module M
          form Input for C
            field value
          feature F
            slice StateChange Change
              command C
                value String
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_accept_the_form_as_an_issuer() => Findings.ShouldBeEmpty();
}
