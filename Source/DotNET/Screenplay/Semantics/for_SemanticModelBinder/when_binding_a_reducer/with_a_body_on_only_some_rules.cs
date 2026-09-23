// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_reducer;

// One rule with code makes it reduction code rather than a projection in disguise, so the projection hint would mislead.
public class with_a_body_on_only_some_rules : given.a_semantic_binder
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
                  file Reducers/Deposited.cs
                on AmountWithdrawn
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_keep_the_unsupported_semantic_syntax_code() => Reducer.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_it_requires_a_portable_reducer_contract() => Reducer.Message.ShouldEqual("Reducer 'Balance' requires a portable reducer contract.");

    Diagnostic Reducer => _result.Diagnostics.Single(_ => _.Message.StartsWith("Reducer 'Balance'", StringComparison.Ordinal));
}
