// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// Cratis/Screenplay#1012 let a feature or a module add buttons to its screens' toolbar once; Screenplay 6 writes that as an
// extending outlet of the template's actions outlet.
public class when_compiling_feature_and_module_outlets : Specification
{
    const string Source =
        """
        module Shop
          screen template Page
            category Test
            header
            main
            outlet shell of actions in header
            outlet actions of actions in header
          outlet shell extends
            button help title "Help" slice Shop.Orders.PlaceOrder emphasis quiet
          feature Orders
            outlet actions extends
              button cancel title "Cancel order" slice CancelOrder icon trash
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
    OutletSyntax FeatureActions => Feature.Outlets.Single();
    ToolbarButtonSyntax Button(int index) => (ToolbarButtonSyntax)FeatureActions.Items.ElementAt(index);

    [Fact] void should_succeed() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_report_nothing() => _result.Diagnostics.Where(_ => _.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_read_the_feature_outlet() => (FeatureActions.Name, FeatureActions.Mode).ShouldEqual(("actions", OutletMode.Extend));
    [Fact] void should_read_the_button_linked_to_a_slice() => (Button(0).Slice, Button(0).Title, Button(0).Icon).ShouldEqual(("CancelOrder", "Cancel order", "trash"));
    [Fact] void should_read_the_confirmation_of_a_button() => Button(0).Confirm.ShouldEqual("Cancel this order?");
    [Fact] void should_read_a_localized_confirmation() => (Button(1).Id, Button(1).Confirm).ShouldEqual(("place", "$strings.orders.confirmPlace"));
    [Fact] void should_read_the_module_outlet() => Module.Outlets.Single().Items.Single().Id.ShouldEqual("help");
}
