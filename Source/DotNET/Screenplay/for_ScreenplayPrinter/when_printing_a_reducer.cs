// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_reducer : given.a_printer
{
    // A read model standing on its own, and a reducer naming it with the same arrow a projection uses.
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
                description "What the account is worth right now"
                balance Decimal
                movements Int

              reducer Balance => AccountBalance
                on AmountDeposited
                  csharp
                    ```
                    return context.State is null
                        ? new(context.Event.amount, 1)
                        : context.State with { balance = context.State.balance + context.Event.amount };
                    ```
                on AmountWithdrawn
                  file Reducers/Withdrawn.cs
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_declare_the_read_model() => ReadModel.Name.ShouldEqual("AccountBalance");
    [Fact] void should_keep_the_read_model_shape() => ReadModel.Properties.Select(_ => _.Name).ShouldContainOnly("balance", "movements");
    [Fact] void should_keep_the_read_model_description() => ReadModel.Description.ShouldEqual("What the account is worth right now");

    // The builder names its target, never the other way round - one arrow, one direction.
    [Fact] void should_point_the_reducer_at_the_read_model() => Reducer.ReadModel.ShouldEqual("AccountBalance");
    [Fact] void should_keep_every_rule() => Reducer.Rules.Select(_ => _.Event).ShouldContainOnly("AmountDeposited", "AmountWithdrawn");
    [Fact] void should_keep_the_inline_reduction() => Reducer.Rules.First().Code!.Code.ShouldContain("context.State is null");
    [Fact] void should_keep_the_file_reduction() => Reducer.Rules.Last().File!.Path.ShouldEqual("Reducers/Withdrawn.cs");
    [Fact] void should_print_the_arrow() => _roundtrip.Printed.ShouldContain("reducer Balance => AccountBalance");

    ReadModelSyntax ReadModel => Slice.ReadModels!.Single();

    ReducerSyntax Reducer => Slice.Reducers!.Single();

    SliceSyntax Slice => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single();
}
