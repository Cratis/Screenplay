// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_ability_answers : given.a_printer
{
    const string Source =
        """
        concept ParcelId : Uuid

        system Carrier
          ability BookPickup once
            parcelId ParcelId
            returns
              trackingNumber String
            outcome NoCapacity

        module Shipping
          feature Pickup
            slice Automation Booking
              event OrderPacked
                parcelId ParcelId
              specification S
                given Carrier.BookPickup returns
                  trackingNumber = "TRK-1"
                given Carrier.BookPickup answers NoCapacity
                given Carrier.BookPickup fails "down \"hard\""
                when append OrderPacked
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then invokes Carrier.BookPickup
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse_to_the_same_diagnostics() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(_roundtrip.Original.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_returned_values() => _roundtrip.Printed.ShouldContain("given Carrier.BookPickup returns\n");
    [Fact] void should_print_the_outcome() => _roundtrip.Printed.ShouldContain("given Carrier.BookPickup answers NoCapacity\n");
    [Fact] void should_print_the_quoted_reason() => _roundtrip.Printed.ShouldContain("given Carrier.BookPickup fails \"down \\\"hard\\\"\"\n");
    [Fact] void should_keep_the_syntax() => SyntaxJson.Serialize(_roundtrip.Reparsed.Value!).GetRawText().ShouldEqual(SyntaxJson.Serialize(_roundtrip.Original.Value!).GetRawText());
}
