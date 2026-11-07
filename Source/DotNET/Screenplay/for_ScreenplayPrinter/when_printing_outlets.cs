// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_outlets : given.a_printer
{
    const string Source =
        """
        layout AppShell
          category Test
          content page routed primary
          side panel
          outlet sidebar of navigation in side
            logo image "/assets/acme.svg" title "Acme"
            default products title "Products" target Catalog.Products
              item drafts title "Drafts" target Catalog.Products

        module Catalog
          screen template ListPage
            category Test
            fits slot content
            header
            main
            detail pane routed by sku optional
            outlet toolbar of actions
              default refresh title "Refresh" runs refresh
              default export title "Export"
            outlet rowActions of actions for item
            outlet filters of filters
              default search title "Search"
            outlet stats of stats
            outlet detailTabs of tabs in detail

          dialog template TaskDialog
            category Test
            body
            outlet actions of actions
              default cancel title "Cancel" runs close
              default save title "Save" emphasis primary runs submit

          outlet sidebar extends
            item archive title "Archive" target Catalog.Products

          feature Products
            outlet toolbar overrides
              hide export
              button add title "Add product" slice DraftProduct emphasis primary
                before refresh
              group more title "More" icon dots
                button withdraw title "Withdraw" slice WithdrawProduct
            outlet filters extends
              filter status title "Status" sets AllProducts.status
            outlet stats extends
              stat drafts title "Drafts" value from ProductStats.drafts

            slice StateChange DraftProduct
              command DraftProduct
                sku String identifier
                produces ProductDrafted
                  sku = sku
              event ProductDrafted
                sku String

            slice StateChange WithdrawProduct
              command WithdrawProduct
                sku String identifier
                produces ProductWithdrawn
                  sku = sku
              event ProductWithdrawn
                sku String

            slice StateView ProductCatalog
              readmodel ProductRow
                sku String
                status String
              query AllProducts => ProductRow[]
                filter status String
              screen ProductCatalog
                template ListPage
                outlet toolbar inherits

            slice StateView ProductTotals
              readmodel ProductStats
                drafts Int
              query ProductStats => ProductStats
              screen ProductTotals

            slice StateView ProductPricing
              readmodel PriceRow
                sku String
              query Prices => PriceRow[]
              screen ProductPricing

          feature Pricing
            contribute to detailTabs
              tab pricing title "Pricing" shows ProductPricing

        ui profile Desktop
          target platform web
          root template AppShell
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_errors() => _roundtrip.Original!.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_reparse_to_the_same_diagnostics() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(_roundtrip.Original!.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_a_navigation_outlet_in_a_slot() => _roundtrip.Printed.ShouldContain("  outlet sidebar of navigation in side\n    logo image \"/assets/acme.svg\" title \"Acme\"\n    default products title \"Products\" target Catalog.Products\n      item drafts title \"Drafts\" target Catalog.Products\n");
    [Fact] void should_print_default_actions_with_built_ins() => _roundtrip.Printed.ShouldContain("    outlet toolbar of actions\n      default refresh title \"Refresh\" runs refresh\n      default export title \"Export\"\n");
    [Fact] void should_print_an_outlet_for_the_item() => _roundtrip.Printed.ShouldContain("    outlet rowActions of actions for item\n");
    [Fact] void should_print_a_tabs_outlet_in_a_slot() => _roundtrip.Printed.ShouldContain("    outlet detailTabs of tabs in detail\n");
    [Fact] void should_print_the_dialog_buttons() => _roundtrip.Printed.ShouldContain("      default save title \"Save\" emphasis primary runs submit\n");
    [Fact] void should_print_an_override_with_a_hidden_item() => _roundtrip.Printed.ShouldContain("    outlet toolbar overrides\n      hide export\n      button add title \"Add product\" slice DraftProduct emphasis primary\n        before refresh\n");
    [Fact] void should_print_a_group() => _roundtrip.Printed.ShouldContain("      group more title \"More\" icon dots\n        button withdraw title \"Withdraw\" slice WithdrawProduct\n");
    [Fact] void should_print_a_filter() => _roundtrip.Printed.ShouldContain("      filter status title \"Status\" sets AllProducts.status\n");
    [Fact] void should_print_a_stat() => _roundtrip.Printed.ShouldContain("      stat drafts title \"Drafts\" value from ProductStats.drafts\n");
    [Fact] void should_print_a_contributed_tab() => _roundtrip.Printed.ShouldContain("    contribute to detailTabs\n      tab pricing title \"Pricing\" shows ProductPricing\n");
    [Fact] void should_print_a_screen_that_inherits_an_outlet() => _roundtrip.Printed.ShouldContain("        outlet toolbar inherits\n");
    [Fact] void should_read_the_outlet_kind() => Toolbar.Kind.ShouldEqual(OutletKind.Actions);
    [Fact] void should_read_the_defaults() => Toolbar.Items.All(item => item.IsDefault).ShouldBeTrue();
    [Fact] void should_read_the_built_in() => ((ToolbarButtonSyntax)Toolbar.Items.First()).Runs.ShouldEqual("refresh");
    [Fact] void should_read_the_subject() => Template.Outlets.Single(outlet => outlet.Name == "rowActions").Subject.ShouldEqual(ScreenActionSubjectKind.Item);
    [Fact] void should_read_the_slot() => Template.Outlets.Single(outlet => outlet.Name == "detailTabs").Slot.ShouldEqual("detail");
    [Fact] void should_read_the_mode() => FeatureToolbar.Mode.ShouldEqual(OutletMode.Override);
    [Fact] void should_read_the_hidden_item() => FeatureToolbar.Hides.Single().Id.ShouldEqual("export");
    [Fact] void should_read_the_position() => FeatureToolbar.Items.First().Position!.Placement.ShouldEqual(OutletPlacement.Before);
    [Fact] void should_read_the_filter_binding() => ((OutletFilterSyntax)Products.Outlets.Single(outlet => outlet.Name == "filters").Items.Single()).Parameter.ShouldEqual("status");

    ScreenTemplateSyntax Template => _roundtrip.Reparsed.Value!.Modules.Single().ScreenTemplates.Single();

    OutletSyntax Toolbar => Template.Outlets.Single(outlet => outlet.Name == "toolbar");

    FeatureSyntax Products => _roundtrip.Reparsed.Value!.Modules.Single().Features.First();

    OutletSyntax FeatureToolbar => Products.Outlets.Single(outlet => outlet.Name == "toolbar");
}
