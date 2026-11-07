// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// Each placement, hook, picker and binding check, on the placement document that is otherwise valid.
public class when_compiling_placement_and_hooks_that_are_wrong : given.a_placement_document
{
    const string Hook = "      button x title \"X\" ";

    [Fact] void should_refuse_running_a_slice_whose_input_needs_a_form() => Codes(Source(feature: Hook + "slice SetProductPrice for item\n        runs\n")).ShouldContainOnly(DiagnosticCodes.RunsHookNeedsForm);
    [Fact] void should_refuse_running_without_a_subject_for_the_identifier() => Codes(Source(feature: Hook + "slice WithdrawProduct\n        runs\n")).ShouldContainOnly(DiagnosticCodes.RunsHookNeedsForm);
    [Fact] void should_refuse_opening_a_screenless_slice_that_cannot_run() => Codes(Source(feature: Hook + "slice WithdrawProduct\n")).ShouldContainOnly(DiagnosticCodes.HookToScreenlessSlice);
    [Fact] void should_refuse_naming_a_place_for_a_screenless_slice() => Codes(Source(feature: Hook + "slice WithdrawProduct for item opens drawer\n")).ShouldContainOnly(DiagnosticCodes.HookToScreenlessSlice);
    [Fact] void should_name_runs_as_the_fix_when_the_inputs_resolve() => new ScreenplayCompiler().Compile(Source(feature: Hook + "slice WithdrawProduct for item opens drawer\n")).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.HookToScreenlessSlice).Message.ShouldContain("write 'runs'");
    [Fact] void should_run_a_screenless_slice_whose_inputs_resolve() => Codes(Source(feature: Hook + "slice WithdrawProduct for item\n")).ShouldBeEmpty();
    [Fact] void should_refuse_a_slot_the_template_does_not_declare() => Codes(Source(feature: Hook + "slice SetProductPrice for item opens in slot ghost\n")).ShouldContainOnly(DiagnosticCodes.HookSlotNotDeclared);
    [Fact] void should_refuse_a_slot_whose_role_the_slice_does_not_fit() => Codes(Source(feature: Hook + "slice SetProductPrice for item opens in slot activity\n")).ShouldContainOnly(DiagnosticCodes.HookSlotRoleNotFitted);
    [Fact] void should_refuse_a_slot_the_screen_already_fills() => Codes(Source(screen: "        toolbar extra\n          button x title \"X\" slice SetProductPrice for item opens in slot main\n")).ShouldContainOnly(DiagnosticCodes.HookSlotAlreadyFilled);
    [Fact] void should_refuse_reading_the_item_for_the_screen() => Codes(Source(feature: Hook + "slice SetProductPrice for screen\n        enabled when item.status == \"x\"\n")).ShouldContainOnly(DiagnosticCodes.EnabledWhenWithoutItem);
    [Fact] void should_refuse_running_and_opening() => Codes(Source(feature: Hook + "slice WithdrawProduct for item opens dialog\n        runs\n")).ShouldContain(DiagnosticCodes.InvalidToolbarButton);
    [Fact] void should_refuse_running_a_built_in_and_the_slice() => Codes(Source(feature: Hook + "slice WithdrawProduct for item runs refresh\n        runs\n")).ShouldContain(DiagnosticCodes.InvalidToolbarButton);
    [Fact] void should_refuse_an_unknown_opening() => Codes(Source(feature: Hook + "slice SetProductPrice opens window\n")).ShouldContain(DiagnosticCodes.InvalidToolbarButton);
    [Fact] void should_refuse_an_unknown_place_to_fit() => Codes(Source().Replace("fits dialog, drawer, pane", "fits dialog, sideways", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidScreenFits);
    [Fact] void should_refuse_a_place_fitted_twice() => Codes(Source().Replace("fits dialog, drawer, pane", "fits dialog, dialog", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidScreenFits);
    [Fact] void should_refuse_a_second_fits_line() => Codes(Source().Replace("fits dialog, drawer, pane", "fits dialog\n        fits page", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidScreenFits);
    [Fact] void should_refuse_a_template_fits_slot_on_a_screen() => Codes(Source().Replace("fits dialog, drawer, pane", "fits slot main", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidScreenFits);
    [Fact] void should_refuse_choosing_from_a_single_result_query() => Codes(Source().Replace("choose from query AllCategories show name", "choose from query ProductBySku show sku", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidFormFieldChoices);
    [Fact] void should_refuse_showing_a_field_the_rows_do_not_have() => Codes(Source().Replace("show name", "show ghost", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidFormFieldChoices);
    [Fact] void should_refuse_choosing_from_an_unknown_query() => Codes(Source().Replace("choose from query AllCategories", "choose from query Ghost", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.UnknownQuery);
    [Fact] void should_refuse_a_malformed_choice() => Codes(Source().Replace("choose from query AllCategories show name", "choose AllCategories", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidFormFieldChoices);
    [Fact] void should_refuse_showing_an_unknown_screen() => Codes(Source().Replace("show ProductDetails by", "show Ghost by", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidScreenShow);
    [Fact] void should_refuse_showing_from_an_unknown_element() => Codes(Source().Replace("from products.selection", "from ghost.selection", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidScreenShow);
    [Fact] void should_refuse_showing_by_a_parameter_the_screen_is_not_keyed_by() => Codes(Source().Replace("show ProductDetails by sku", "show ProductDetails by id", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidScreenShow);
    [Fact] void should_refuse_showing_outside_a_template_slot() => Codes(Source(screen: "        show ProductDetails by sku from products.selection\n")).ShouldContainOnly(DiagnosticCodes.InvalidScreenShow);
    [Fact] void should_refuse_a_malformed_show() => Codes(Source().Replace("from products.selection", "from products", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidScreenShow);
    [Fact] void should_warn_about_a_component_bound_to_nothing() => Codes(Source().Replace("            column status\n", "            column status\n            component Cratis.Components.Text featured\n", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.UnboundScreenText);
    [Fact] void should_accept_a_component_with_static_text() => Codes(Source().Replace("            column status\n", "            column status\n            component Cratis.Components.Text featured\n              property text = \"Featured\"\n", StringComparison.Ordinal)).ShouldBeEmpty();
}
