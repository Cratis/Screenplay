// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions.for_ExtensionContractParser;

public class when_parsing_a_complete_contract : Specification
{
    CompilationResult<ExtensionContract> _result;

    static string Join<T>(IEnumerable<T> values) => string.Join("|", values.Select(value => value?.ToString() ?? "-"));

    void Because() => _result = ExtensionContractParser.Parse(ExtensionContractSample.Stripe);

    [Fact] void should_report_nothing() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_manifest() => Join([_result.Value!.DisplayName, _result.Value.Description, _result.Value.Icon]).ShouldEqual("Stripe payments|Card payments with Stripe|assets/icon.svg");
    [Fact] void should_read_the_images_with_their_captions() => Join(_result.Value!.Images.Select(image => image.Caption)).ShouldEqual("The payment form on a checkout screen|-");
    [Fact] void should_read_the_requirements() => Join(_result.Value!.Requirements.Select(requirement => $"{requirement.Kind} {requirement.Value}")).ShouldEqual("stage ^5|platform web");
    [Fact] void should_read_the_settings_with_their_exposure() => Join(_result.Value!.Settings.Select(setting => setting.Exposure)).ShouldEqual("Public|Secret|Backend");
    [Fact] void should_read_the_abilities() => Join(_result.Value!.Abilities.Select(ability => $"{ability.Name} {ability.EffectClass} {ability.Returns.Count()}")).ShouldEqual("CreatePaymentIntent idempotent 1");
    [Fact] void should_read_the_triggers() => Join(_result.Value!.Triggers.Single().Payload.Select(field => field.Name)).ShouldEqual("paymentIntentId");
    [Fact] void should_read_the_component() => Component().ShouldEqual("react placeholders/payment-form.html web amount succeeded CreatePaymentIntent");
    [Fact] void should_read_the_backend_and_frontend() => Join([_result.Value!.Backend!.Runtime, _result.Value.Backend.Version, _result.Value.Backend.Assembly, _result.Value.Frontend]).ShouldEqual("dotnet|9|Cratis.Extensions.Stripe.dll|frontend/index.js");
    [Fact] void should_read_the_activation() => $"{_result.Value!.Activation!.Always}|{_result.Value.Activation.IdleAmount}|{_result.Value.Activation.IdleUnit}".ShouldEqual("False|10|minute");

    string Component()
    {
        var component = _result.Value!.Components.Single();
        return $"{component.Kind} {component.Placeholder} {Join<string>(component.Platforms)} {Join<string>(component.Properties.Select(property => property.Name))} {component.Signals.Single().Name} {Join<string>(component.Invokes)}";
    }
}
