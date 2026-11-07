// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_reading_retired_screenplay_5_forms : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          category Shell
          navigation contributes Navigation
          content
          navigation topbar
            logo image "/logo.svg" title "Shop"
            item orders title "Orders" target Sales
            button add title "Add" slice Sales.Orders.Add
          arrangement flow
            row
              navigation
              content

        module Sales
          screen template Page
            fits slot content
            category Page
            toolbar
              toolbar actions
                button refresh title "Refresh" runs refresh
              component App.Search search
            main
          navigation sidebar extends
            item open title "Open" target Sales.Orders
          toolbar actions
            button refresh title "Refresh" runs refresh
          feature Orders
            toolbar actions
              item cancel action Cancel
            slice StateView Browse
              screen Browse
                component App.Card card
                  outlet detail
        """;

    CompilationResult<ApplicationSyntax> _result;
    LayoutSyntax _layout;
    ModuleSyntax _module;

    void Because()
    {
        _result = _compiler.Parse(Source);
        _layout = _result.Value!.Layouts!.Single();
        _module = _result.Value.Modules.Single();
    }

    [Fact] void should_keep_the_renamed_component_outlet() => _result.Value!.RetiredForms.OfType<RetiredOutlet>().Single().Place.ShouldEqual(RetiredOutletPlace.Component);
    [Fact] void should_keep_the_slot_contribution() => _layout.RetiredForms.OfType<RetiredSlotContribution>().Single().Point.ShouldEqual("Navigation");
    [Fact] void should_still_declare_the_contributing_slot() => _layout.Slots.Select(slot => slot.Name).ShouldContain("navigation");
    [Fact] void should_keep_the_layout_region() => _layout.RetiredForms.OfType<RetiredNavigationRegion>().Single().Region.ShouldEqual("topbar");
    [Fact] void should_read_the_region_logo() => _layout.RetiredForms.OfType<RetiredNavigationRegion>().Single().Logo!.Title.ShouldEqual("Shop");
    [Fact] void should_read_the_region_items_and_buttons_in_order() => _layout.RetiredForms.OfType<RetiredNavigationRegion>().Single().Items.Select(item => item.Id).ShouldEqual(["orders", "add"]);
    [Fact] void should_keep_the_module_region_with_its_mode() => _module.RetiredForms.OfType<RetiredNavigationRegion>().Single().Mode.ShouldEqual(OutletMode.Extend);
    [Fact] void should_keep_the_module_toolbar() => _module.RetiredForms.OfType<RetiredToolbar>().Single().Toolbar.Buttons.Single().Id.ShouldEqual("refresh");
    [Fact] void should_keep_the_feature_toolbar() => _module.Features.Single().RetiredForms.OfType<RetiredToolbar>().Single().Toolbar.Items.Single().Name.ShouldEqual("cancel");
    [Fact] void should_keep_the_template_slot_holding_items() => _module.ScreenTemplates.Single().RetiredForms.OfType<RetiredSlotItems>().Single().Toolbar!.Buttons.Single().Id.ShouldEqual("refresh");
    [Fact] void should_keep_what_else_the_template_slot_holds() => _module.ScreenTemplates.Single().RetiredForms.OfType<RetiredSlotItems>().Single().Others.Count.ShouldEqual(1);
    [Fact] void should_report_nothing_inside_a_retired_block() => _result.Diagnostics.Count(diagnostic => diagnostic.Location.Line is 6 or 7 or 8 or 19 or 20 or 21 or 24 or 26 or 29).ShouldEqual(0);
    [Fact] void should_not_serialize_the_retired_forms() => Syntax.Serialization.SyntaxJson.Serialize(_result.Value!).GetRawText().Contains("RetiredForms", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
}
