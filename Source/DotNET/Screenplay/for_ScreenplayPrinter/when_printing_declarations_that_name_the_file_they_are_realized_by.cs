// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_declarations_that_name_the_file_they_are_realized_by : given.a_printer
{
    const string Source =
        """
        domain Invoicing

        concept InvoiceId : Uuid
          file Invoicing/InvoiceId.cs

        type InvoiceLine
          file Invoicing/InvoiceLine.cs
          amount Decimal

        trigger LedgerFileArrived
          description "A file arrived from the ledger"
          file Integrations/LedgerFileTrigger.cs
          name

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              description "Registers an invoice"
              file Invoicing/RegisterInvoice/RegisterInvoice.cs

              command RegisterInvoice
                invoiceId InvoiceId identifier

                produces InvoiceRegistered
                  invoiceId = invoiceId

              event InvoiceRegistered
                file Invoicing/RegisterInvoice/RegisterInvoice.cs
                invoiceId InvoiceId

              readmodel Invoice
                file Invoicing/RegisterInvoice/Invoice.cs
                invoiceId InvoiceId

              projection Invoices => Invoice
                file Invoicing/RegisterInvoice/Invoices.cs
                from InvoiceRegistered
                  invoiceId = invoiceId

              specification RegisteringAnInvoice
                file Invoicing/RegisterInvoice/when_registering_an_invoice.cs
                when RegisterInvoice
                  invoiceId = "5c1f2f0a-1a9f-4f47-9f39-0f6f5f7f5f5f"
                then InvoiceRegistered
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_every_file_reference_it_was_given() => _roundtrip.Printed.Split('\n').Count(_ => _.TrimStart().StartsWith("file ", StringComparison.Ordinal)).ShouldEqual(8);
    [Fact] void should_print_the_concept_under_its_header() => _roundtrip.Printed.ShouldContain("concept InvoiceId : Uuid\n  file Invoicing/InvoiceId.cs");
    [Fact] void should_print_the_type_under_its_header() => _roundtrip.Printed.ShouldContain("type InvoiceLine\n  file Invoicing/InvoiceLine.cs");
    [Fact] void should_print_the_trigger_after_its_description() => _roundtrip.Printed.ShouldContain("description \"A file arrived from the ledger\"\n  file Integrations/LedgerFileTrigger.cs");
    [Fact] void should_print_the_slice_after_its_description() => _roundtrip.Printed.ShouldContain("description \"Registers an invoice\"\n      file Invoicing/RegisterInvoice/RegisterInvoice.cs");
    [Fact] void should_print_the_event_under_its_header() => _roundtrip.Printed.ShouldContain("event InvoiceRegistered\n        file Invoicing/RegisterInvoice/RegisterInvoice.cs");
    [Fact] void should_print_the_readmodel_under_its_header() => _roundtrip.Printed.ShouldContain("readmodel Invoice\n        file Invoicing/RegisterInvoice/Invoice.cs");
    [Fact] void should_print_the_projection_under_its_header() => _roundtrip.Printed.ShouldContain("projection Invoices => Invoice\n        file Invoicing/RegisterInvoice/Invoices.cs");
    [Fact] void should_print_the_specification_under_its_header() => _roundtrip.Printed.ShouldContain("specification RegisteringAnInvoice\n        file Invoicing/RegisterInvoice/when_registering_an_invoice.cs");
    [Fact] void should_preserve_the_concept_file() => Concept.File!.Path.ShouldEqual("Invoicing/InvoiceId.cs");
    [Fact] void should_preserve_the_type_file() => Type.File!.Path.ShouldEqual("Invoicing/InvoiceLine.cs");
    [Fact] void should_preserve_the_trigger_file() => Trigger.File!.Path.ShouldEqual("Integrations/LedgerFileTrigger.cs");
    [Fact] void should_preserve_the_slice_file() => Slice.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/RegisterInvoice.cs");
    [Fact] void should_preserve_the_event_file() => Event.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/RegisterInvoice.cs");
    [Fact] void should_preserve_the_readmodel_file() => ReadModel.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/Invoice.cs");
    [Fact] void should_preserve_the_projection_file() => Projection.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/Invoices.cs");
    [Fact] void should_preserve_the_specification_file() => Specification.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/when_registering_an_invoice.cs");

    ConceptSyntax Concept => _roundtrip.Reparsed.Value!.Concepts.Single();
    TypeSyntax Type => _roundtrip.Reparsed.Value!.Types!.Single();
    TriggerSyntax Trigger => _roundtrip.Reparsed.Value!.Triggers!.Single();
    SliceSyntax Slice => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single();
    EventSyntax Event => Slice.Events.Single();
    ReadModelSyntax ReadModel => Slice.ReadModels!.Single();
    ProjectionSyntax Projection => Slice.Projections.Single();
    SpecificationSyntax Specification => Slice.Specifications.Single();
}
