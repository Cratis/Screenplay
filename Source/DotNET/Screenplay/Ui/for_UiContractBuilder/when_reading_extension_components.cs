// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_extension_components : Specification
{
    const string Model = """
        system Payments
          extension "cratis/stripe" version 2

        module M
          feature F
            slice StateChange ConfirmPayment
              event PaymentConfirmed
                paymentIntentId String
              command ConfirmPayment
                paymentIntentId String
                produces PaymentConfirmed
            slice StateView Checkout
              screen Checkout
                component Payments.PaymentForm payment
                  property amount = "10"
                  on succeeded
                    execute ConfirmPayment
                      with paymentIntentId from signal.paymentIntentId
        """;

    ExtensionFixture _fixture;
    UiScreen _screen;

    void Establish()
    {
        _fixture = ExtensionFixture.Locked("cratis/stripe", "2", ExtensionContractSample.Stripe);
        var placeholders = Path.Combine(Path.GetDirectoryName(_fixture.Cache.PathOf(_fixture.Digest))!, "placeholders");
        Directory.CreateDirectory(placeholders);
        File.WriteAllText(Path.Combine(placeholders, "payment-form.html"), "<p>Pay</p>");
        var syntax = new ScreenplayCompiler().Compile(Model).Value!;
        var resolution = _fixture.Resolver().Resolve(syntax, []);
        _screen = UiContractBuilder.For(syntax, null, "Shop", resolution).Screens.Single(screen => screen.Name == "Checkout");
    }

    void Destroy() => _fixture.Dispose();

    UiExtensionComponent Extension => _screen.Scene!.Components.Single().Extension!;

    [Fact] void should_resolve_the_component_through_the_system() => (Extension.System, Extension.Reference, Extension.Component).ShouldEqual(("Payments", "cratis/stripe", "PaymentForm"));
    [Fact] void should_carry_what_the_contract_says_about_it() => (Extension.Kind, Extension.Placeholder).ShouldEqual(("react", "placeholders/payment-form.html"));
    [Fact] void should_carry_the_signals_the_screen_handles() => Extension.Handles!.Select(handler => (handler.Signal, string.Join(",", handler.Commands!))).ShouldContainOnly([("succeeded", "ConfirmPayment")]);
    [Fact] void should_say_which_extensions_the_screen_needs() => _screen.Extensions.ShouldContainOnly([new UiExtensionNeed("Payments", "cratis/stripe")]);
    [Fact] void should_carry_the_static_placeholder_the_package_holds() => Extension.PlaceholderHtml.ShouldEqual("<p>Pay</p>");
}
