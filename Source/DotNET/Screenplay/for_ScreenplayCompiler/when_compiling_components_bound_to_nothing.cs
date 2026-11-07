// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// PLAY0912 is for text that should show bound data and shows sample text instead - not for every component without
/// properties. A form's input or a component the user picks a selection from is not text left unbound.
/// </summary>
public class when_compiling_components_bound_to_nothing : given.a_placement_document
{
    const string FormAction = "        action DraftProduct for screen\n";
    const string Component = "        component Cratis.Components.SearchBox search\n";

    [Fact] void should_warn_on_a_screen_that_shows_state() => Codes(Source().Replace("            column status\n", "            column status\n            component Cratis.Components.Text featured\n", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.UnboundScreenText);
    [Fact] void should_not_warn_on_a_form_screen_without_data() => Codes(Source().Replace(FormAction, FormAction + Component, StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_warn_on_a_form_screen_that_binds_data() => Codes(Source().Replace(FormAction, FormAction + Component + "        data CategoryRow[] via query AllCategories\n", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.UnboundScreenText);
    [Fact] void should_not_warn_on_a_component_a_slot_shows_the_selection_of() => Codes(Source().Replace("table products\n              column sku\n              column status\n", "component Cratis.Components.Gallery products\n", StringComparison.Ordinal)).ShouldBeEmpty();
}
