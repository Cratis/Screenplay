// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_read_model_built_twice : given.a_compiler
{
    // A projection and a reducer both claiming the same read model. Either could have produced the value a
    // reader is looking at, and nothing in the document says which - so it is an error rather than a warning.
    const string Source =
        """
        module Billing
          feature Accounts
            slice StateView Balance
              event AmountDeposited
                amount Decimal

              readmodel AccountBalance
                balance Decimal

              projection Deposits => AccountBalance
                from AmountDeposited key amount
                  balance = amount

              reducer Balance => AccountBalance
                on AmountDeposited
                  file Reducers/Deposited.cs
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_it_once() => _result.Diagnostics.Count(_ => _.Code == DiagnosticCodes.ReadModelBuiltMoreThanOnce).ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() =>
        _result.Diagnostics.Single(_ => _.Code == DiagnosticCodes.ReadModelBuiltMoreThanOnce).Severity.ShouldEqual(DiagnosticSeverity.Error);

    // Still parses both, so a tool can show the reader what the conflict is between.
    [Fact] void should_keep_the_projection() => Slice.Projections.Single().ReadModel.ShouldEqual("AccountBalance");
    [Fact] void should_keep_the_reducer() => Slice.Reducers!.Single().ReadModel.ShouldEqual("AccountBalance");

    SliceSyntax Slice => _result.Value!.Modules.Single().Features.Single().Slices.Single();
}
