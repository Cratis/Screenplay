// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_toolbar_buttons : given.a_printer
{
    const string Source =
        """
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              screen List
                toolbar actions
                  item legacy action PlaceOrder
                    label "Legacy"
                  button place title "Place \"new\" order" slice Shop.Orders.PlaceOrder emphasis primary icon plus
                  button "nav:later" title $strings.orders.later slice Orders.PlaceOrder emphasis quiet
                  button idle title "Idle"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    ScreenToolbarSyntax Toolbar => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "OrderList").Screens.Single().Directives.OfType<ScreenToolbarSyntax>().Single();

    [Fact] void should_compile_without_errors() => _roundtrip.Original!.Diagnostics.Any(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_reparse_to_the_same_diagnostics() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(_roundtrip.Original!.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_a_button_with_every_part() => _roundtrip.Printed.ShouldContain("button place title \"Place \\\"new\\\" order\" slice Shop.Orders.PlaceOrder emphasis primary icon plus");
    [Fact] void should_quote_an_id_that_is_not_an_identifier() => _roundtrip.Printed.ShouldContain("button \"nav:later\" title $strings.orders.later slice Orders.PlaceOrder emphasis quiet");
    [Fact] void should_print_a_button_without_a_slice() => _roundtrip.Printed.ShouldContain("button idle title \"Idle\"\n");
    [Fact] void should_print_the_items_before_the_buttons() => _roundtrip.Printed.IndexOf("item legacy", StringComparison.Ordinal).ShouldBeLessThan(_roundtrip.Printed.IndexOf("button place", StringComparison.Ordinal));
    [Fact] void should_preserve_the_buttons() => Toolbar.Buttons.Select(button => button.Id).ShouldContainOnly("place", "nav:later", "idle");
    [Fact] void should_preserve_the_title_text() => Toolbar.Buttons.First().Title.ShouldEqual("Place \"new\" order");
    [Fact] void should_preserve_the_items() => Toolbar.Items.Single().Name.ShouldEqual("legacy");
}
