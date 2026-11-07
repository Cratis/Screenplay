// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_placement_and_hooks : given.a_printer
{
    const string Source =
        """
        module Catalog
          screen template ListPage
            category Lists
            main
            detail pane routed by sku optional
            outlet rowActions of actions for item

          form DraftProductForm for DraftProduct
            field category
              choose from query AllCategories show name

          feature Products
            outlet rowActions extends
              button price title "Set price" slice DraftProduct opens in slot detail
              button draft title "Draft" slice DraftProduct for screen opens drawer
              button withdraw title "Withdraw" slice WithdrawProduct
                runs
                enabled when item.status == "onSale" and item.stock > 0
                confirm "Withdraw this product?"

            slice StateChange DraftProduct
              command DraftProduct
                sku String identifier
                category String
                produces ProductDrafted
                  for sku
              event ProductDrafted
                sku String
              screen DraftProduct
                fits drawer, pane, dialog
                action DraftProduct for screen

            slice StateChange WithdrawProduct
              command WithdrawProduct
                sku String identifier
                produces ProductWithdrawn
                  for sku
              event ProductWithdrawn
                sku String

            slice StateView ProductCatalog
              readmodel CategoryRow
                name String
              query AllCategories => CategoryRow[]
              screen ProductCatalog
                template ListPage
                  main
                    table products
                      column name
                  detail
                    show ProductCatalog by sku from products.selection
                toolbar extra
                  item export action WithdrawProduct
                    enabled when item.name == "x"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_errors() => _roundtrip.Original!.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_reparse_to_the_same_diagnostics() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(_roundtrip.Original!.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_a_hook_that_opens_in_a_slot() => _roundtrip.Printed.ShouldContain("      button price title \"Set price\" slice DraftProduct opens in slot detail\n");
    [Fact] void should_print_the_subject_before_the_opening() => _roundtrip.Printed.ShouldContain("      button draft title \"Draft\" slice DraftProduct for screen opens drawer\n");
    [Fact] void should_print_runs_enabled_when_and_confirm_beneath_the_button() => _roundtrip.Printed.ShouldContain("      button withdraw title \"Withdraw\" slice WithdrawProduct\n        runs\n        enabled when item.status == \"onSale\" and item.stock > 0\n        confirm \"Withdraw this product?\"\n");
    [Fact] void should_print_where_a_screen_fits() => _roundtrip.Printed.ShouldContain("      screen DraftProduct\n        fits drawer, pane, dialog\n");
    [Fact] void should_print_a_picker_beneath_its_field() => _roundtrip.Printed.ShouldContain("    field category\n      choose from query AllCategories show name\n");
    [Fact] void should_print_what_a_slot_shows() => _roundtrip.Printed.ShouldContain("          detail\n            show ProductCatalog by sku from products.selection\n");
    [Fact] void should_print_when_a_toolbar_item_is_enabled() => _roundtrip.Printed.ShouldContain("          item export action WithdrawProduct\n            enabled when item.name == \"x\"\n");
}
