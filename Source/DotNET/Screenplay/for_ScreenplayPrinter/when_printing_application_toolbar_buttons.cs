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
          navigation
          outlet Navigation of navigation in navigation
          content
          outlet topbar of navigation
            default shop title "Shop" target Shop
          outlet appActions of actions in topbar
            default newProduct title "New product" slice Shop.Orders.PlaceOrder emphasis primary icon plus
            default adjust title $strings.nav.adjust slice Orders.PlaceOrder
              authorize IsBuyer
        module Shop
          outlet appActions extends
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
    [Fact] void should_print_the_outlets_in_the_order_written() => _roundtrip.Printed.IndexOf("default shop", StringComparison.Ordinal).ShouldBeLessThan(_roundtrip.Printed.IndexOf("default newProduct", StringComparison.Ordinal));
    [Fact] void should_print_a_button_with_every_part() => _roundtrip.Printed.ShouldContain("default newProduct title \"New product\" slice Shop.Orders.PlaceOrder emphasis primary icon plus");
    [Fact] void should_print_the_authorize_line_under_its_button() => _roundtrip.Printed.ShouldContain("default adjust title $strings.nav.adjust slice Orders.PlaceOrder\n      authorize IsBuyer");
    [Fact] void should_print_the_module_buttons() => _roundtrip.Printed.ShouldContain("outlet appActions extends\n    button more title \"More\" slice PlaceOrder");
}
