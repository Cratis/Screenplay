// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_localized_requirements : given.a_printer
{
    const string Source =
        """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                quantity Int
                validate
                  require quantity > 0
                    message $strings.orders.positiveQuantity
                  require quantity < 10
                    message "Too many"
                    severity warning
                  require quantity != 5
                    severity information
                    message $strings.orders.notFive
                  require quantity != 6
                    message "Not six"
        """;

    RoundTripResult _roundtrip;
    RequirementSyntax[] _requirements;

    void Because()
    {
        _roundtrip = RoundTrip(Source);
        _requirements = [.. _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Requirements!];
    }

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_identically_twice() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_keys_unquoted() => _roundtrip.Printed.ShouldContain("message $strings.orders.positiveQuantity");
    [Fact] void should_print_a_key_after_severity_unquoted() => _roundtrip.Printed.ShouldContain("message $strings.orders.notFive");
    [Fact] void should_print_literal_messages_quoted() => _roundtrip.Printed.ShouldContain("message \"Too many\"");
    [Fact] void should_keep_both_keys() => _requirements.Count(_ => _.Message?.StartsWith("$strings.", StringComparison.Ordinal) is true).ShouldEqual(2);
    [Fact] void should_keep_severity_in_either_order() => _requirements.Select(_ => _.Severity).SequenceEqual([ValidationSeverity.Error, ValidationSeverity.Warning, ValidationSeverity.Information, ValidationSeverity.Error]).ShouldBeTrue();
}
