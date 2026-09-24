// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_comments_in_projections_and_screens : given.a_printer
{
    const string Source =
        """
        module Sales
          feature Orders
            slice StateView ListOrders
              // @derived projection
              projection OrderList
                // source event
                from OrderPlaced // @owner sales
                  status = status // mapping annotation
              // screen explanation
              screen Orders
                title "Orders" // translated later
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_keep_projection_comments() => _roundtrip.Printed.ShouldContain("// @derived projection");
    [Fact] void should_keep_projection_mapping_comments() => _roundtrip.Printed.ShouldContain("status = status // mapping annotation");
    [Fact] void should_keep_screen_comments() => _roundtrip.Printed.ShouldContain("title \"Orders\" // translated later");
    [Fact] void should_be_stable() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
