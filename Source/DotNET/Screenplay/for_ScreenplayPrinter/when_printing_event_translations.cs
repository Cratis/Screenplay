// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_event_translations : given.a_printer
{
    const string Source = """
        import Shipping.ShipmentDispatched from "shipping"
        module Shipping
          feature Tracking
            slice Translate Publish
              direction outbound
              event OrderPacked
              public event OrderShipped
                orderId String
              projection OrderShippedPublisher => OrderShipped // publishes
                from OrderPacked
                  orderId = $eventSourceId
            slice Translate Track
              direction inbound
              event Dispatched
              capture ShipmentTracking
                source events // feed
                  from ShipmentDispatched
                key orderId
                append Dispatched
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_accept_source() => _roundtrip.Original!.Success.ShouldBeTrue();
    [Fact] void should_accept_printed_source() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_preserve_syntax() => SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _roundtrip.Reparsed.Value!).ShouldBeTrue();
    [Fact] void should_print_idempotently() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_event_target() => _roundtrip.Printed.ShouldContain("projection OrderShippedPublisher => OrderShipped");
    [Fact] void should_print_the_events_source() => _roundtrip.Printed.ShouldContain("source events");
    [Fact] void should_print_the_consumed_event() => _roundtrip.Printed.ShouldContain("from ShipmentDispatched");
}
