// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_claim_condition;

public class and_the_target_is_a_path : for_ScreenplayPrinter.given.a_printer
{
    const string Source =
        """
        policy IsSameDepartment
          require claim "department" matches invoice.department
        """;

    for_ScreenplayPrinter.given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_target_as_a_path() => Target(_roundtrip.Original!).ShouldBeOfExactType<PathExpressionSyntax>();
    [Fact] void should_print_the_target_unquoted() => _roundtrip.Printed.ShouldContain(@"require claim ""department"" matches invoice.department");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_the_target_as_a_path() => Target(_roundtrip.Reparsed).ShouldBeOfExactType<PathExpressionSyntax>();
    [Fact] void should_preserve_the_path() => ((PathExpressionSyntax)Target(_roundtrip.Reparsed)).Path.ShouldEqual("invoice.department");
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static ExpressionSyntax Target(CompilationResult<ApplicationSyntax> result) =>
        ((ClaimConditionSyntax)result.Value!.Policies.Single().Condition!).Matches!;
}
