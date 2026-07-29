// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_specification_with_an_unnamed_error : given.a_printer
{
    const string Source =
        """
        module Identity
          feature Tokens
            slice StateChange ExchangeToken
              command ExchangeToken
                token String

              specification WhenExchangingAndMagicLinkIsNotActive
                when ExchangeToken
                then error
                then error "Token has expired"
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_an_unnamed_reason_without_quotes() => _roundtrip.Printed.ShouldContain("then error\n");
    [Fact] void should_print_a_named_reason_with_quotes() => _roundtrip.Printed.ShouldContain("then error \"Token has expired\"");
    [Fact] void should_not_turn_an_unnamed_reason_into_an_empty_one() => Errors().First().Name.ShouldBeNull();
    [Fact] void should_preserve_the_named_reason() => Errors().Last().Name.ShouldEqual("Token has expired");

    IEnumerable<SpecificationErrorSyntax> Errors() =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single()
            .Specifications.Single().ThenErrors;
}
