// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_comments_in_forms : given.a_printer
{
    const string Source = """
        module Shop
          // Module-level comment above the form
          form PlaceOrderForm for PlaceOrder
            // @input orderId — new identity supplied by the client
            // @input productId — selected product row
            populate from item
            // quantity input
            field quantity label "Quantity"
            // end of form
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_keep_form_comments_in_their_original_order_and_scope() => _roundtrip.Printed.ShouldContain("  // Module-level comment above the form\n  form PlaceOrderForm for PlaceOrder\n    // @input orderId — new identity supplied by the client\n    // @input productId — selected product row\n    populate from item\n    // quantity input\n    field quantity label \"Quantity\"\n    // end of form");
    [Fact] void should_not_duplicate_comments() => _roundtrip.Printed.Split("// @input orderId", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_stay_stable_after_reparsing() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
