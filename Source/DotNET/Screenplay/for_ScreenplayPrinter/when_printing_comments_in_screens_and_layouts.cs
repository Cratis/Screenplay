// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_comments_in_screens_and_layouts : given.a_printer
{
    const string Source = """
        layout Main
          // layout slot
          content
          arrangement flow
            // arranged slot
            content
        module Shop
          screen template Shell
            // template slot
            main
          feature Ordering
            slice StateView ViewOrder
              screen Order
                action PlaceOrder
                  label "Place"
                  // action navigation
                  navigate to Summary
                table Orders
                  // row navigation
                  on row-click navigate to Summary
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_keep_layout_comments_with_their_slots() => _roundtrip.Printed.ShouldContain("  // layout slot\n  content\n\n  arrangement flow\n    // arranged slot\n    content");
    [Fact] void should_keep_template_slot_comment() => _roundtrip.Printed.ShouldContain("  screen template Shell\n    // template slot\n    main");
    [Fact] void should_keep_action_navigation_comment_inside_action() => _roundtrip.Printed.ShouldContain("          label \"Place\"\n          // action navigation\n          navigate to Summary");
    [Fact] void should_keep_table_navigation_comment_inside_table() => _roundtrip.Printed.ShouldContain("          // row navigation\n          on row-click navigate to Summary");
    [Fact] void should_stay_stable_after_reparsing() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
