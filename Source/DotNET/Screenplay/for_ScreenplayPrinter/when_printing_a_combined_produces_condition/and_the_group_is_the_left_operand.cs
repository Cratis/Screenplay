// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_combined_produces_condition;

public class and_the_group_is_the_left_operand : for_ScreenplayPrinter.given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid
                status    String
                currency  String
                terms     String

                produces when (status == "sent" or currency == "NOK") and terms == "net30"
                  InvoiceFlagged
                    invoiceId = invoiceId

              event InvoiceFlagged
                invoiceId Uuid
        """;

    // The group is what the author wrote and it is not what precedence would have given, so dropping it
    // when printing hands back a different condition - the defect this construct shipped with.
    const string Shape = "((status == sent Or currency == NOK) And terms == net30)";

    for_ScreenplayPrinter.given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_group_the_left_operand_when_parsing() => Describe(Condition(_roundtrip.Original!)).ShouldEqual(Shape);
    [Fact] void should_print_the_group() => _roundtrip.Printed.ShouldContain(@"produces when (status == ""sent"" or currency == ""NOK"") and terms == ""net30""");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_to_the_same_tree() => Describe(Condition(_roundtrip.Reparsed)).ShouldEqual(Shape);
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static ConditionSyntax Condition(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Produces.Single().When!;

    static string Describe(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => $"{comparison.Left} == {Describe(comparison.Right)}",
        LogicalConditionSyntax logical => $"({Describe(logical.Left)} {logical.Operator} {Describe(logical.Right)})",
        _ => "?"
    };

    static string Describe(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => literal.Value?.ToString() ?? "?",
        _ => "?"
    };
}
