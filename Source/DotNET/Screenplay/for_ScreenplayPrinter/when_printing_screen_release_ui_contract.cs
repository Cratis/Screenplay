// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_screen_release_ui_contract : given.a_printer
{
    const string Source =
        """
        layout ApplicationShell
          category application
          type masterDetail
          exposes selectedItem String
          outlet details
          navigation contributes Navigation
          content

        ui profile Web
          target platform web

          packages
            scene.web

          icons
            scene.icons

          layout ApplicationShell

        template ApplicationShell

        module Sales
          template FeatureShell

          screen template FeatureShell
            fits slot content
            category feature
            type masterDetail
            exposes selectedInvoice String
            outlet detail
            list
            detail

          form EditInvoice for EditInvoice
            field invoiceId
            columns manual
              column invoiceId label "Invoice"
              column customerName label "Customer"

          feature Invoices
            template FeatureShell

            slice StateView BrowseInvoices
              template FeatureShell

              command EditInvoice
                invoiceId String
                customerName String

              readmodel InvoiceList
                invoiceId String
                customerName String

              query AllInvoices => InvoiceList[]

              screen BrowseInvoices
                data InvoiceList via query AllInvoices
                toolbar main
                  item edit action EditInvoice
                    label "Edit"
                    icon edit
                    presentation placement "primary"
                  item details navigate to InvoiceDetails
                    parameter invoiceId from selectedInvoice.id
                component scene.web.DataGrid invoices
                  context invoices
                  property selectedItem from selectedInvoice
                  property title = "Invoices"
                  icon table
                  presentation density "compact"
                  exposes selectedInvoice from selectedItem
                  outlet detail
                    summary selectedInvoice
                      field invoiceId label "Invoice"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_component_identity() => Component.Component.ShouldEqual("scene.web.DataGrid");
    [Fact] void should_preserve_component_property_binding() => Component.Properties.First().Binding.ShouldEqual("selectedInvoice");
    [Fact] void should_preserve_component_literal_property() => Component.Properties.Last().Value.ShouldEqual("Invoices");
    [Fact] void should_preserve_component_outlet() => Component.Outlets.Single().Directives.Single().ShouldBeOfExactType<ScreenSummarySyntax>();
    [Fact] void should_preserve_toolbar_presentation() => Toolbar.Items.First().Presentation.Single().Value.ShouldEqual("primary");
    [Fact] void should_preserve_manual_form_columns() => Form.Columns.Last().Label.ShouldEqual("Customer");
    [Fact] void should_preserve_profile_icons() => _roundtrip.Reparsed.Value!.UiProfiles!.Single().Icons.Single().ShouldEqual("scene.icons");
    [Fact] void should_preserve_scoped_template_assignments() => Slice.Templates.Single().Name.ShouldEqual("FeatureShell");

    SliceSyntax Slice => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single();
    FormSyntax Form => _roundtrip.Reparsed.Value!.Modules.Single().Forms!.Single();
    ScreenComponentSyntax Component => (ScreenComponentSyntax)Slice.Screens.Single().Directives.Last();
    ScreenToolbarSyntax Toolbar => (ScreenToolbarSyntax)Slice.Screens.Single().Directives.Skip(1).First();
}
