// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_claim_condition;

public class and_the_target_is_a_literal : for_ScreenplayPrinter.given.a_printer
{
    for_ScreenplayPrinter.given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Application(new LiteralExpressionSyntax("central", SourceLocation.Start)));

    [Fact] void should_quote_the_target() => _roundtrip.Printed.ShouldContain(@"require claim ""branch"" matches ""central""");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_the_target_as_a_literal() => Target(_roundtrip.Reparsed).ShouldBeOfExactType<LiteralExpressionSyntax>();
    [Fact] void should_preserve_the_value() => ((LiteralExpressionSyntax)Target(_roundtrip.Reparsed)).Value.ShouldEqual("central");
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static ExpressionSyntax Target(CompilationResult<ApplicationSyntax> result) =>
        ((ClaimConditionSyntax)result.Value!.Policies.Single().Condition!).Matches!;

    static ApplicationSyntax Application(ExpressionSyntax target) =>
        new(
            [],
            [],
            [
                new PolicySyntax(
                    "IsCentralBranch",
                    new ClaimConditionSyntax("branch", false, target, SourceLocation.Start),
                    null,
                    SourceLocation.Start)
            ],
            [],
            SourceLocation.Start);
}
