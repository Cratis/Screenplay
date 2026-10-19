// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_pinned_generated_values : given.a_printer
{
    const string Source =
        """
        concept ShipmentId : Uuid
        module Shipping
          feature Dispatch
            slice Automation Dispatching
              event OrderConfirmed
                orderId String
              command CreateShipment
                shipmentId ShipmentId generated identifier
              specification S
                given generated CreateShipment.shipmentId = "11111111-1111-4111-8111-111111111111"
                given generated CreateShipment.shipmentId = "22222222-2222-4222-8222-222222222222"
                when append OrderConfirmed
                  orderId = "o1"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse_to_the_same_diagnostics() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(_roundtrip.Original!.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_every_pinned_value_in_order() => _roundtrip.Printed.ShouldContain("given generated CreateShipment.shipmentId = \"11111111-1111-4111-8111-111111111111\"\n        given generated CreateShipment.shipmentId = \"22222222-2222-4222-8222-222222222222\"");
}
