// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// Cratis/Screenplay#1012: a feature or a module adds buttons to the toolbar slot of its screens, each linked to a command of the feature.
public class when_compiling_feature_and_module_toolbars : Specification
{
    const string Source =
        """
        module Shop
          toolbar shell
            button help title "Help" slice Shop.Orders.PlaceOrder emphasis quiet
          feature Orders
            toolbar actions
              item cancel action CancelOrder
                label "Cancel order"
                icon trash
                confirm "Cancel this order?"
              button place title "Place order" slice Shop.Orders.PlaceOrder emphasis primary icon plus
                confirm $strings.orders.confirmPlace
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
            slice StateChange CancelOrder
              command CancelOrder
                orderId Uuid identifier
                produces OrderCancelled
                  orderId = orderId
              event OrderCancelled
                orderId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    ModuleSyntax Module => _result.Value!.Modules.Single();
    FeatureSyntax Feature => Module.Features.Single();
    ScreenToolbarSyntax FeatureToolbar => Feature.Toolbars.Single();

    [Fact] void should_succeed() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_report_nothing() => _result.Diagnostics.Where(_ => _.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_read_the_feature_toolbar() => FeatureToolbar.Name.ShouldEqual("actions");
    [Fact] void should_read_the_item_linked_to_a_command() => (FeatureToolbar.Items.Single().Target, FeatureToolbar.Items.Single().Label, FeatureToolbar.Items.Single().Icon).ShouldEqual(("CancelOrder", "Cancel order", "trash"));
    [Fact] void should_read_the_confirmation_of_an_item() => FeatureToolbar.Items.Single().Confirm.ShouldEqual("Cancel this order?");
    [Fact] void should_read_the_button_and_its_confirmation() => (FeatureToolbar.Buttons.Single().Id, FeatureToolbar.Buttons.Single().Confirm).ShouldEqual(("place", "$strings.orders.confirmPlace"));
    [Fact] void should_read_the_module_toolbar() => Module.Toolbars.Single().Buttons.Single().Id.ShouldEqual("help");
}
