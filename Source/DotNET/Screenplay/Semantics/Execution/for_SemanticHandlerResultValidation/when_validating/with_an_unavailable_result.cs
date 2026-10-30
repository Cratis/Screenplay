// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_an_unavailable_result : given.a_possible_events_handler
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_distinguish_unavailable_from_a_violation(bool diagnostic)
    {
        var result = diagnostic ? SemanticCommandBodyResult.Unsupported("x") : new SemanticCommandBodyResult();
        var outcome = SemanticHandlerResultValidation.Validate(Plan, Command, Values, result);
        outcome.Unavailable.ShouldEqual(diagnostic ? "x" : "handler returned no v2 result");
        outcome.Violation.ShouldBeNull();
        outcome.Items.ShouldBeEmpty();
    }

    [Fact]
    void should_give_the_diagnostic_precedence_over_payloads()
    {
        var outcome = SemanticHandlerResultValidation.Validate(Plan, Command, Values, SemanticCommandBodyResult.Produced([Item("Placed").Payload]) with { Diagnostic = "x" });
        outcome.Unavailable.ShouldEqual("x");
        outcome.Violation.ShouldBeNull();
    }
}
