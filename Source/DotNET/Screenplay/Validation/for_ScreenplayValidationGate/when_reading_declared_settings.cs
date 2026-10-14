// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_reading_declared_settings : given.a_gate_file
{
    ValidationGateReadResult _result;

    void Establish() => File.WriteAllText(GatePath, """
        {
          "$schema": "https://example.test/schema.json",
          "validation": {
            "checks": ["navigation", "PLAY0534", "all", "navigation"],
            "warningsAsErrors": false,
            "scope": "M.F.S",
            "openQuestions": "report"
          }
        }
        """);

    void Because() => _result = ScreenplayValidationGate.Read(Root);

    [Fact] void should_preserve_the_ordered_selections() => _result.Declaration.Checks.Value.ToArray().ShouldEqual(new[] { "navigation", "PLAY0534", "all", "navigation" });
    [Fact] void should_read_an_explicit_false() => _result.Declaration.WarningsAsErrors.ShouldEqual(false);
    [Fact] void should_read_the_scope() => _result.Declaration.Scope.ShouldEqual("M.F.S");
    [Fact] void should_read_the_questions_mode() => _result.Declaration.OpenQuestions.ShouldEqual("report");
    [Fact] void should_not_refuse() => _result.Failure.ShouldBeNull();
}
