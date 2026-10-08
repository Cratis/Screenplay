// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_invalid_screen_release_bindings : given.a_printer
{
    const string Source =
        """
        module Sales
          feature Invoices
            slice StateView BrowseInvoices
              screen BrowseInvoices
                component scene.web.DataGrid invoices
                  property selectedItem from component invoices
                  property title from data name carry forever
        """;

    RoundTripResult _roundtrip;
    ComponentPropertySyntax _invalidComponent;
    ComponentPropertySyntax _unsupportedModifier;

    void Because()
    {
        _roundtrip = RoundTrip(Source);
        var properties = global::Cratis.Screenplay.given.SyntaxNodes.Under(_roundtrip.Original!.Value!).OfType<ComponentPropertySyntax>().ToArray();
        _invalidComponent = properties[0];
        _unsupportedModifier = properties[1];
    }

    [Fact] void should_report_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldNotBeEmpty();
    [Fact] void should_preserve_invalid_component_binding_raw_text() => _invalidComponent.Binding!.RawText.ShouldEqual("from component invoices");
    [Fact] void should_preserve_unsupported_modifier_raw_text() => _unsupportedModifier.Binding!.RawText.ShouldEqual("carry forever");
    [Fact] void should_print_invalid_text_back_to_source() => _roundtrip.Printed.ShouldContain("property selectedItem from component invoices");
    [Fact] void should_serialize_invalid_raw_text() => SyntaxJson.Serialize(_invalidComponent.Binding!).GetRawText().ShouldContain("rawText");
}
