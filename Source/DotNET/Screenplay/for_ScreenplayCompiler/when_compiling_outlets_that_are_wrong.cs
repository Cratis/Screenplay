// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// Each outlet check and each Screenplay 5 form an outlet replaced, on a model that is otherwise valid.
public class when_compiling_outlets_that_are_wrong : Specification
{
    const string Shell =
        """
        layout Shell
          category Shell
          content page routed primary
          side panel
          outlet sidebar of navigation in side
            default home title "Home" target Shop

        module Shop
          screen template Page
            category Page
            fits slot content
            header
            main
            outlet actions of actions in header
              default refresh title "Refresh" runs refresh
            outlet filters of filters in header
            outlet stats of stats in header
            outlet tabs of tabs in main

          feature Orders
        {{feature}}
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  for orderId
              event OrderPlaced
                orderId Uuid
              screen PlaceOrder
                action PlaceOrder

            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              query AllOrders => OrderRow[]
                filter status String
              query OrderTotals => OrderRow
              screen OrderList
                template Page
        """;

    static IEnumerable<string> Codes(string feature) =>
        new ScreenplayCompiler().Compile(Shell.Replace("{{feature}}", feature, StringComparison.Ordinal)).Diagnostics
            .Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information)
            .Select(diagnostic => diagnostic.Code);

    static IEnumerable<string> CodesOf(string source) =>
        new ScreenplayCompiler().Compile(source).Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code);

    [Fact] void should_accept_the_outlets_as_written() => Codes(string.Empty).ShouldBeEmpty();
    [Fact] void should_accept_every_change() => Codes("""
            outlet actions extends
              hide refresh
              button place title "Place" slice PlaceOrder
            outlet filters extends
              filter status title "Status" sets AllOrders.status
            outlet stats overrides
              stat total title "Total" value from OrderTotals.orderId
            outlet tabs extends
              tab list title "List" shows OrderList
        """).ShouldBeEmpty();
    [Fact] void should_refuse_hiding_an_item_the_outlet_does_not_have() => Codes("    outlet actions extends\n      hide ghost").ShouldContainOnly(DiagnosticCodes.OutletItemNotInherited);
    [Fact] void should_refuse_placing_before_an_item_the_outlet_does_not_have() => Codes("    outlet actions extends\n      button place title \"Place\" slice PlaceOrder\n        before ghost").ShouldContainOnly(DiagnosticCodes.OutletItemNotInherited);
    [Fact] void should_accept_placing_before_an_inherited_item() => Codes("    outlet actions extends\n      button place title \"Place\" slice PlaceOrder\n        before refresh").ShouldBeEmpty();
    [Fact] void should_refuse_an_item_of_the_wrong_kind() => Codes("    outlet actions extends\n      tab t title \"T\" shows OrderList").ShouldContainOnly(DiagnosticCodes.OutletItemWrongKind);
    [Fact] void should_refuse_a_filter_on_a_parameter_the_query_does_not_have() => Codes("    outlet filters extends\n      filter f title \"F\" sets AllOrders.ghost").ShouldContainOnly(DiagnosticCodes.OutletFilterParameterUnknown);
    [Fact] void should_refuse_a_stat_from_a_list_query() => Codes("    outlet stats extends\n      stat s title \"S\" value from AllOrders.orderId").ShouldContainOnly(DiagnosticCodes.OutletStatFromListQuery);
    [Fact] void should_refuse_an_unknown_outlet() => Codes("    outlet ghost extends").ShouldContainOnly(DiagnosticCodes.UnknownOutlet);
    [Fact] void should_refuse_a_contribution_to_an_unknown_outlet() => Codes("    contribute to ghost\n      tab t title \"T\" shows OrderList").ShouldContainOnly(DiagnosticCodes.UnknownOutlet);
    [Fact] void should_accept_a_contribution_to_any_outlet() => Codes("    contribute to tabs\n      tab t title \"T\" shows OrderList").ShouldBeEmpty();
    [Fact] void should_refuse_a_contributed_item_of_the_wrong_kind() => Codes("    contribute to tabs\n      button b title \"B\" slice PlaceOrder").ShouldContainOnly(DiagnosticCodes.OutletItemWrongKind);
    [Fact] void should_refuse_an_outlet_changed_twice() => Codes("    outlet actions extends\n    outlet actions overrides\n      button b title \"B\" slice PlaceOrder").ShouldContainOnly(DiagnosticCodes.DuplicateOutlet);
    [Fact] void should_refuse_repeating_an_inherited_id() => Codes("    outlet actions extends\n      button refresh title \"Again\" slice PlaceOrder").ShouldContainOnly(DiagnosticCodes.DuplicateOutletItem);
    [Fact] void should_refuse_a_declaration_below_a_template() => Codes("    outlet more of actions").ShouldContainOnly(DiagnosticCodes.InvalidOutletDeclaration);
    [Fact] void should_refuse_a_mode_on_a_template() => CodesOf("module M\n  screen template T\n    category C\n    main\n    outlet a extends\n").ShouldContainOnly(DiagnosticCodes.InvalidOutletDeclaration);
    [Fact] void should_refuse_an_unknown_kind() => CodesOf("module M\n  screen template T\n    category C\n    main\n    outlet a of widgets\n").ShouldContainOnly(DiagnosticCodes.InvalidOutletDeclaration);
    [Fact] void should_refuse_a_template_outlet_declared_twice() => CodesOf("module M\n  screen template T\n    category C\n    main\n    outlet a of actions\n    outlet a of tabs\n").ShouldContainOnly(DiagnosticCodes.DuplicateOutlet);
    [Fact] void should_refuse_a_line_that_is_not_a_default() => CodesOf("module M\n  screen template T\n    category C\n    main\n    outlet a of actions\n      button b title \"B\"\n").ShouldContainOnly(DiagnosticCodes.InvalidOutletItem);
    [Fact] void should_refuse_a_layout_navigation_region() => CodesOf("layout L\n  category C\n  content\n  navigation sidebar\n    item a title \"A\"\n").ShouldContainOnly(DiagnosticCodes.RemovedNavigationRegion);
    [Fact] void should_refuse_a_module_navigation_region() => CodesOf("module M\n  navigation sidebar extends\n    item a title \"A\" target M\n  feature F\n").ShouldContainOnly(DiagnosticCodes.RemovedNavigationRegion);
    [Fact] void should_refuse_a_slot_contribution_point() => CodesOf("layout L\n  category C\n  nav contributes Navigation\n  content\n").ShouldContainOnly(DiagnosticCodes.RemovedSlotContribution);
    [Fact] void should_refuse_a_feature_toolbar() => CodesOf("module M\n  feature F\n    toolbar actions\n      button b title \"B\"\n").ShouldContainOnly(DiagnosticCodes.RemovedFeatureToolbar);
    [Fact] void should_name_the_migration() => new ScreenplayCompiler().Compile("module M\n  feature F\n    toolbar actions\n").Diagnostics.Single().Message.ShouldContain("screenplay migrate --from 5 --to 6");
    [Fact] void should_name_the_outlet_that_replaces_a_feature_toolbar() => new ScreenplayCompiler().Compile("module M\n  feature F\n    toolbar actions\n").Diagnostics.Single().Message.ShouldContain("'outlet actions extends'");
    [Fact] void should_name_the_outlet_that_replaces_a_slot_contribution_point() => new ScreenplayCompiler().Compile("layout L\n  category C\n  nav contributes Navigation\n  content\n").Diagnostics.Single().Message.ShouldContain("'outlet Navigation of navigation in nav'");
}
