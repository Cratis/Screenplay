// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_reducer;

// A reducer that only lists events has nothing to fold. It stays rejected, but the author is told it is a
// projection written as a reducer rather than that a portable reducer contract is missing.
public class without_a_body : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Accounts
            slice StateView Balance
              event AmountDeposited
                amount Decimal

              event AmountWithdrawn
                amount Decimal

              readmodel AccountBalance
                balance Decimal

              reducer Balance => AccountBalance
                on AmountDeposited
                on AmountWithdrawn
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_reject_the_reducer() => Reducer.Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_keep_the_unsupported_semantic_syntax_code() => Reducer.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_it_has_no_transition_body() => Reducer.Message.ShouldContain("Reducer 'Balance' has no transition body");
    [Fact] void should_point_at_the_projection_form() => Reducer.Message.ShouldContain("declare 'projection <Name> => AccountBalance'");
    [Fact] void should_point_at_the_rule_bodies() => Reducer.Message.ShouldContain("give each 'on <Event>' an inline code block or 'file <path>'");
    [Fact] void should_not_blame_a_missing_reducer_contract() => Reducer.Message.ShouldNotContain("portable reducer contract");

    Diagnostic Reducer => _result.Diagnostics.Single(_ => _.Message.StartsWith("Reducer 'Balance'", StringComparison.Ordinal));
}
