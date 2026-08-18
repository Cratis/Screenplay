// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_declarations_that_name_the_file_they_are_realized_by : given.a_compiler
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

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_concept_file() => Concept.File!.Path.ShouldEqual("Invoicing/InvoiceId.cs");
    [Fact] void should_carry_the_type_file() => Type.File!.Path.ShouldEqual("Invoicing/InvoiceLine.cs");
    [Fact] void should_carry_the_trigger_file() => Trigger.File!.Path.ShouldEqual("Integrations/LedgerFileTrigger.cs");
    [Fact] void should_carry_the_slice_file() => Slice.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/RegisterInvoice.cs");
    [Fact] void should_carry_the_event_file() => Event.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/RegisterInvoice.cs");
    [Fact] void should_carry_the_readmodel_file() => ReadModel.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/Invoice.cs");
    [Fact] void should_carry_the_projection_file() => Projection.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/Invoices.cs");
    [Fact] void should_carry_the_specification_file() => Specification.File!.Path.ShouldEqual("Invoicing/RegisterInvoice/when_registering_an_invoice.cs");
    [Fact] void should_keep_the_trigger_value_the_directive_did_not_take() => Trigger.Data.Single().Name.ShouldEqual("name");
    [Fact] void should_keep_the_type_property_the_directive_did_not_take() => Type.Properties.Single().Name.ShouldEqual("amount");
    [Fact] void should_keep_the_event_property_the_directive_did_not_take() => Event.Properties.Single().Name.ShouldEqual("invoiceId");
    [Fact] void should_keep_the_readmodel_property_the_directive_did_not_take() => ReadModel.Properties.Single().Name.ShouldEqual("invoiceId");
    [Fact] void should_keep_the_projection_body_the_directive_did_not_take() => Projection.Blocks.Count().ShouldEqual(1);
    [Fact] void should_keep_the_specification_body_the_directive_did_not_take() => Specification.When!.CommandType.ShouldEqual("RegisterInvoice");
    [Fact] void should_leave_the_command_without_one() => Slice.Commands.Single().Handler.ShouldBeNull();

    ConceptSyntax Concept => _result.Value!.Concepts.Single();
    TypeSyntax Type => _result.Value!.Types!.Single();
    TriggerSyntax Trigger => _result.Value!.Triggers!.Single();
    SliceSyntax Slice => _result.Value!.Modules.Single().Features.Single().Slices.Single();
    EventSyntax Event => Slice.Events.Single();
    ReadModelSyntax ReadModel => Slice.ReadModels!.Single();
    ProjectionSyntax Projection => Slice.Projections.Single();
    SpecificationSyntax Specification => Slice.Specifications.Single();
}
