// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_toolbar_buttons : given.a_toolbar_document
{
    void Because() => CompileWith("button place title \"Place order\" slice Shop.Orders.PlaceOrder emphasis primary icon plus", "button invoice title $strings.orders.invoice slice Billing.PlaceOrder", "button later title \"Later\"", "item legacy action PlaceOrder");

    ToolbarButtonSyntax Button(string id) => Toolbar.Buttons.Single(button => button.Id == id);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_only_the_missing_slice_warning() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).Where(code => string.CompareOrdinal(code, "PLAY0760") >= 0).ShouldContainOnly(DiagnosticCodes.ToolbarButtonWithoutSlice);
    [Fact] void should_read_every_button() => Toolbar.Buttons.Select(button => button.Id).ShouldContainOnly("place", "invoice", "later");
    [Fact] void should_read_the_title_slice_emphasis_and_icon() => (Button("place").Title, Button("place").Slice, Button("place").Emphasis, Button("place").Icon).ShouldEqual(("Place order", "Shop.Orders.PlaceOrder", ToolbarEmphasis.Primary, "plus"));
    [Fact] void should_read_a_localized_title_as_written() => Button("invoice").Title.ShouldEqual("$strings.orders.invoice");
    [Fact] void should_resolve_a_suffix_of_the_slice_path() => Button("invoice").Slice.ShouldEqual("Billing.PlaceOrder");
    [Fact] void should_leave_optional_parts_absent() => (Button("later").Slice, Button("later").Emphasis, Button("later").Icon).ShouldEqual((null, null, null));
    [Fact] void should_keep_the_action_items_beside_the_buttons() => Toolbar.Items.Single().Name.ShouldEqual("legacy");
}
