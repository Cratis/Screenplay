// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Ui.for_ExecutableScreens;

public class when_admitting_screens_with_extension_components : Specification
{
    const string Source = """
        concept Money : String

        system Payments
          extension "cratis/stripe" version 2

        module Orders
          feature Checkout
            slice StateChange ConfirmPayment
              command ConfirmPayment
                paymentIntentId Uuid identifier
                produces PaymentConfirmed
                  for paymentIntentId
                  paymentIntentId = paymentIntentId
              event PaymentConfirmed
                paymentIntentId Uuid
            slice StateView Checkout
              screen Checkout
                component Payments.PaymentForm payment
                  property amount = "10"
                  on succeeded
                    execute ConfirmPayment
        """;

    ExtensionFixture _fixture;
    ExecutableScreensResult _result;
    SemanticScreen _screen;

    void Establish()
    {
        _fixture = ExtensionFixture.Locked("cratis/stripe", "2", ExtensionContractSample.Stripe);
        ExtensionContractResolver.Current = _fixture.Resolver();
    }

    void Because()
    {
        var workspace = ComparedModel.FromSources("Orders", new Dictionary<string, string> { ["application.play"] = Source }).Workspace;
        _result = ExecutableScreens.Admit(workspace);
        _screen = _result.Compilation?.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Screens).Single()!;
    }

    void Destroy() => _fixture.Dispose();

    [Fact] void should_admit_the_screens() => _result.Refusals.ShouldBeEmpty();
    [Fact] void should_select_esm_v52() => _result.Compilation!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V52);
    [Fact] void should_place_the_component_through_the_system() => _screen.Components.Single().Component.ShouldEqual("PaymentForm");
    [Fact] void should_list_the_properties_the_screen_gives_it() => _screen.Components.Single().Properties.ShouldContainOnly(["amount"]);
    [Fact] void should_handle_the_signal_with_the_command() => _screen.Components.Single().Handles.Single().Signal.ShouldEqual("succeeded");
    [Fact] void should_need_the_extension() => _screen.Extensions.Single().Reference.ShouldEqual("cratis/stripe");
    [Fact] void should_round_trip_through_the_canonical_serializer() => SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Compilation!.Model)).Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Screens).Single().Components.Single().Name.ShouldEqual("payment");
    [Fact] void should_carry_the_placement_into_the_ui_contract() => UiContractBuilder.For(_result.Compilation!.Model).Screens.Single(screen => screen.Name == "Checkout").Extensions!.Single().System.ShouldEqual("Payments");
}
