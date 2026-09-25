// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_form_submit_comments : given.a_printer
{
    const string Source = """
        module Shop
          form OrderForm for PlaceOrder
            // @input orderId
            populate via query GetOrder by orderId // query source
            // after populate
            on submit navigate to Summary // submit destination
            // end of form
          // next form
          form SecondForm for PlaceOrder
            // @input orderId
            field orderId label "Order"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_parse_original() => _roundtrip.Original!.Success.ShouldBeTrue();
    [Fact] void should_reparse() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_keep_comments_on_populate_and_submit_inside_first_form() => _roundtrip.Printed.ShouldContain("  form OrderForm for PlaceOrder\n    // @input orderId\n    populate via query GetOrder by orderId // query source\n    // after populate\n    on submit navigate to Summary // submit destination\n    // end of form");
    [Fact] void should_keep_next_form_comments_outside_first_form() => _roundtrip.Printed.ShouldContain("    // end of form\n\n  // next form\n  form SecondForm for PlaceOrder\n    // @input orderId\n    field orderId label \"Order\"");
    [Fact] void should_keep_both_equal_input_comments() => _roundtrip.Printed.Split("// @input orderId", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_stay_stable_after_reparsing() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
