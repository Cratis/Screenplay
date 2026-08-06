// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_claim_condition;

public class and_the_target_is_a_context_expression : for_ScreenplayPrinter.given.a_printer
{
    const string Source =
        """
        policy IsOwnTenant
          require claim "tenantId" matches $context.tenant
        """;

    for_ScreenplayPrinter.given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_target_as_a_context_expression() => Target(_roundtrip.Original!).ShouldBeOfExactType<ContextExpressionSyntax>();
    [Fact] void should_print_the_target_unquoted() => _roundtrip.Printed.ShouldContain(@"require claim ""tenantId"" matches $context.tenant");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_the_target_as_a_context_expression() => Target(_roundtrip.Reparsed).ShouldBeOfExactType<ContextExpressionSyntax>();
    [Fact] void should_preserve_the_path() => ((ContextExpressionSyntax)Target(_roundtrip.Reparsed)).Path.ShouldEqual("tenant");
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static ExpressionSyntax Target(CompilationResult<ApplicationSyntax> result) =>
        ((ClaimConditionSyntax)result.Value!.Policies.Single().Condition!).Matches!;
}
