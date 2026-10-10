// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_identity_expressions : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(
        """
        policy SameCaller
          require claim "subject" matches $identity.id
        policy LegacyCaller
          require claim "subject" matches $context.identity.id
        """);

    [Fact] void should_parse_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_identity_spelling() => _roundtrip.Printed.ShouldContain("matches $identity.id");
    [Fact] void should_preserve_the_context_spelling() => _roundtrip.Printed.ShouldContain("matches $context.identity.id");
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_again() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_reparse_the_identity_node() => Target(_roundtrip.Reparsed, "SameCaller").ShouldBeOfExactType<IdentityExpressionSyntax>();
    [Fact] void should_preserve_the_identity_path() => ((IdentityExpressionSyntax)Target(_roundtrip.Reparsed, "SameCaller")).Path.ShouldEqual("id");
    [Fact] void should_reparse_the_context_node() => Target(_roundtrip.Reparsed, "LegacyCaller").ShouldBeOfExactType<ContextExpressionSyntax>();
    [Fact] void should_preserve_the_context_path() => ((ContextExpressionSyntax)Target(_roundtrip.Reparsed, "LegacyCaller")).Path.ShouldEqual("identity.id");

    static ExpressionSyntax Target(CompilationResult<ApplicationSyntax> result, string policy) =>
        ((ClaimConditionSyntax)result.Value!.Policies.Single(value => value.Name == policy).Condition!).Matches!;
}
