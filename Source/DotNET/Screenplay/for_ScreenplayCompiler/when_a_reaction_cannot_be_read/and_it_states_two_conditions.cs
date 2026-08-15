// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_a_reaction_cannot_be_read;

public class and_it_states_two_conditions : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange Change
              event InvoicePaid
                amount Decimal

            slice Automation Reconcile
              reaction PaymentReconciler
                when InvoicePaid
                  amount
                where amount > 100
                where amount < 500
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_it() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateReactionCondition);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
