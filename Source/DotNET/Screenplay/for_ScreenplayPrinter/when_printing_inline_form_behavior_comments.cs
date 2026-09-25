// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_inline_form_behavior_comments : given.a_printer
{
    const string Source = """
        module Shop
          form OrderForm for PlaceOrder
            field quantity label "Quantity"
            // submit action belongs inside the form
            on submit
              execute PlaceOrder
            // click action belongs inside the form
            on click
              navigate to Summary
          form OtherForm for PlaceOrder
            field orderId label "Order"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_parse_original_and_reparse() => (_roundtrip.Original!.Success && _roundtrip.Reparsed.Success).ShouldBeTrue();
    [Fact] void should_keep_each_comment_on_its_inline_behavior_inside_the_form() => _roundtrip.Printed.ShouldContain("  form OrderForm for PlaceOrder\n    field quantity label \"Quantity\"\n    // submit action belongs inside the form\n    on submit\n      execute PlaceOrder\n    // click action belongs inside the form\n    on click\n      navigate to Summary");
    [Fact] void should_not_duplicate_comments() => _roundtrip.Printed.Split("// submit action belongs inside the form", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_stay_stable_after_reparsing() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
