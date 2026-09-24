// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_repeated_reads_with_aliases : given.a_printer
{
    const string Source =
        """
        module Banking
          feature Transfers
            slice StateChange Transfer
              command Transfer
                sourceId Uuid
                destinationId Uuid
                reads Account as source by sourceId
                reads Account as destination by destinationId
                validate
                  require source.balance > 0
                    message "Funds required"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_print_both_aliases_before_their_keys() =>
        _roundtrip.Printed.ShouldContain("reads Account as source by sourceId\n");
    [Fact] void should_print_the_second_alias() =>
        _roundtrip.Printed.ShouldContain("reads Account as destination by destinationId\n");
    [Fact] void should_reparse_both_aliases() =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Reads!.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_syntax_tree() =>
        SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _roundtrip.Reparsed.Value!).ShouldBeTrue();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
