// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_a_form : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid
                customerName String
                dueDate Date
                totalAmount Decimal
                lineItems String[]

            slice StateView InvoiceDraft
              query GetInvoiceDraft => InvoiceDraftReadModel

            slice StateView InvoiceList
              screen InvoiceList

          form RegisterInvoiceForm for RegisterInvoice
            populate via query GetInvoiceDraft by invoiceId

            field customerName
            field dueDate label "Due date"
            field totalAmount from calculatedTotal
            field lineItems compose using BuildLineItems

            on submit navigate to InvoiceList by invoiceId
        """;

    CompilationResult<ApplicationSyntax> _result;
    FormSyntax _form;
    List<FormFieldSyntax> _fields;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _form = _result.Value!.Modules.Single().Forms!.Single();
        _fields = [.. _form.Fields];
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_form_name() => _form.Name.ShouldEqual("RegisterInvoiceForm");
    [Fact] void should_parse_the_bound_command() => _form.For.ShouldEqual("RegisterInvoice");
    [Fact] void should_parse_the_populate_query() => ((FormPopulateViaQuerySyntax)_form.Populate!).Query.ShouldEqual("GetInvoiceDraft");
    [Fact] void should_parse_the_populate_by_param() => ((FormPopulateViaQuerySyntax)_form.Populate!).By.ShouldEqual("invoiceId");
    [Fact] void should_parse_the_bare_field_property() => _fields[0].Property.ShouldEqual("customerName");
    [Fact] void should_leave_the_bare_field_unlabeled() => _fields[0].Label.ShouldBeNull();
    [Fact] void should_parse_the_labeled_field() => _fields[1].Label.ShouldEqual("Due date");
    [Fact] void should_parse_the_renamed_field() => _fields[2].From.ShouldEqual("calculatedTotal");
    [Fact] void should_parse_the_composed_field() => _fields[3].ComposeUsing.ShouldEqual("BuildLineItems");
    [Fact] void should_parse_the_submit_screen() => _form.OnSubmit!.Screen.ShouldEqual("InvoiceList");
    [Fact] void should_parse_the_submit_by_param() => _form.OnSubmit!.By.ShouldEqual("invoiceId");
}
