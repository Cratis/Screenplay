// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

/// <summary>
/// A gate written for Screenplay 5 that names PLAY0533 - on by default since Screenplay 6 - is still valid.
/// </summary>
public class when_reading_a_gate_naming_the_check_that_is_on_by_default : given.a_gate_file
{
    ValidationGateReadResult _result;

    void Establish() => File.WriteAllText(GatePath, """
        {
          "validation": {
            "checks": ["PLAY0533"]
          }
        }
        """);

    void Because() => _result = ScreenplayValidationGate.Read(Root);

    [Fact] void should_not_refuse() => _result.Failure.ShouldBeNull();
    [Fact] void should_keep_the_selection() => Assert.NotNull(_result.Declaration.Checks).ToArray().ShouldEqual(["PLAY0533"]);
    [Fact] void should_resolve_to_the_input_surfaces_check() => ValidationGateResolver.Resolve(_result, new()).Gate.Checks.Value.ToArray().ShouldEqual(["input-surfaces"]);
}
