// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_application_toolbar_buttons : given.a_printer
{
    const string Source =
        """
        policy IsBuyer
          require role "buyer"
        layout AppShell
          category application
          topbar
          navigation contributes Navigation
          content
          navigation topbar
            item shop title "Shop" target Shop
            button newProduct title "New product" slice Shop.Orders.PlaceOrder emphasis primary icon plus
            button adjust title $strings.nav.adjust slice Orders.PlaceOrder
              authorize IsBuyer
        module Shop
          navigation topbar extends
            button more title "More" slice PlaceOrder
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse_to_the_same_diagnostics() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(_roundtrip.Original!.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_buttons_after_the_items() => _roundtrip.Printed.IndexOf("item shop", StringComparison.Ordinal).ShouldBeLessThan(_roundtrip.Printed.IndexOf("button newProduct", StringComparison.Ordinal));
    [Fact] void should_print_a_button_with_every_part() => _roundtrip.Printed.ShouldContain("button newProduct title \"New product\" slice Shop.Orders.PlaceOrder emphasis primary icon plus");
    [Fact] void should_print_the_authorize_line_under_its_button() => _roundtrip.Printed.ShouldContain("button adjust title $strings.nav.adjust slice Orders.PlaceOrder\n      authorize IsBuyer");
    [Fact] void should_print_the_module_buttons() => _roundtrip.Printed.ShouldContain("navigation topbar extends\n    button more title \"More\" slice PlaceOrder");
}
