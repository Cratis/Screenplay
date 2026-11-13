// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Extensions.for_ExtensionUseSites;

public class when_a_screen_places_an_extension_component : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    const string Component = """
        concept Money : String

        system Payments
          extension "cratis/stripe" version 2

        ui profile Web
          target platform web
          packages scene.web

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
        {0}
        """;

    ExtensionFixture _fixture;

    void Establish() => _fixture = ExtensionFixture.Locked("cratis/stripe", "2", ExtensionContractSample.Stripe);

    void Destroy() => _fixture.Dispose();

    static string Place(string body) => string.Format(Component, string.Join('\n', body.Split('\n').Select(line => "          " + line)));

    string[] Diagnostics(string body, string? contract = null)
    {
        using var fixture = contract is null ? null : ExtensionFixture.Locked("cratis/stripe", "2", contract);
        ExtensionContractResolver.Current = (fixture ?? _fixture).Resolver();
        var model = Place(body);
        var parsed = new ScreenplayCompiler().Compile(model).Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Code + " " + d.Message).ToArray();
        return [.. parsed, .. Bind(model).Diagnostics.Select(d => d.Code).Where(code => code.StartsWith("PLAY11", StringComparison.Ordinal))];
    }

    [Fact] void should_accept_a_declared_property_and_signal() => Diagnostics("component Payments.PaymentForm payment\n  property amount = \"10\"\n  on succeeded\n    notify info \"paid\"").ShouldBeEmpty();
    [Fact] void should_accept_a_signal_value_in_an_action() => Diagnostics("component Payments.PaymentForm payment\n  on succeeded\n    execute ConfirmPayment\n      with paymentIntentId from signal.paymentIntentId").ShouldBeEmpty();
    [Fact] void should_report_a_value_the_signal_does_not_carry() => Diagnostics("component Payments.PaymentForm payment\n  on succeeded\n    execute ConfirmPayment\n      with paymentIntentId from signal.nope").ShouldContain(DiagnosticCodes.UnknownExtensionSignalValue);
    [Fact] void should_report_an_unknown_component() => Diagnostics("component Payments.Missing payment").ShouldContain(DiagnosticCodes.UnknownExtensionComponent);
    [Fact] void should_report_an_unknown_property() => Diagnostics("component Payments.PaymentForm payment\n  property nope = \"x\"").ShouldContain(DiagnosticCodes.UnknownExtensionComponentProperty);
    [Fact] void should_report_an_unknown_signal() => Diagnostics("component Payments.PaymentForm payment\n  on exploded\n    notify info \"x\"").ShouldContain(DiagnosticCodes.UnknownExtensionSignal);
    [Fact] void should_report_a_platform_the_component_does_not_support() => Diagnostics("component Payments.PaymentForm payment", ExtensionContractSample.Stripe.Replace("platform web\n", "platform ios\n")).ShouldContain(DiagnosticCodes.ExtensionComponentPlatformUnsupported);
}
