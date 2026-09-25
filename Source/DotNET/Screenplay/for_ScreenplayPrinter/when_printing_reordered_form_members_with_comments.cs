// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_reordered_form_members_with_comments : given.a_printer
{
    const string Source = """
        module Shop
          form OrderForm for PlaceOrder
            // quantity belongs to the field
            field quantity label "Quantity"
            // source belongs to populate
            populate from item
            // order belongs to the field
            field orderId label "Order"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_parse_original_and_reparse() => (_roundtrip.Original!.Success && _roundtrip.Reparsed.Success).ShouldBeTrue();
    [Fact] void should_move_comments_with_the_members_they_annotate() => _roundtrip.Printed.ShouldContain("  form OrderForm for PlaceOrder\n    // source belongs to populate\n    populate from item\n    // quantity belongs to the field\n    field quantity label \"Quantity\"\n    // order belongs to the field\n    field orderId label \"Order\"");
    [Fact] void should_stay_stable_after_reparsing() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
