// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_naming_an_incomplete_specification_example : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId String
                produces InvoiceRegistered
                  invoiceId = invoiceId
              event InvoiceRegistered
                invoiceId String
              specification RegisteringAnInvoice
                given Aregisteredinvoice
                when RegisterInvoice
                  invoiceId = "invoice-1"
                then InvoiceRegistered
                  invoiceId = "invoice-1"

              example Aregisteredinvoice : Billing.Invoices.RegisterInvoice.InvoiceRegistered   // no invoiceId value
        """;

    Diagnostic _diagnostic;

    void Because() => _diagnostic = Bind(Source).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.MissingSpecificationProperty);

    [Fact] void should_name_the_specification_instead_of_the_step() => _diagnostic.Message.ShouldEqual("Specification 'RegisteringAnInvoice' using example 'Aregisteredinvoice' for 'Billing.Invoices.RegisterInvoice.InvoiceRegistered' is missing required property 'invoiceId'; supply it in the example or step.");
    [Fact] void should_locate_the_given_step() => _diagnostic.Location.Line.ShouldEqual(11);
    [Fact] void should_keep_the_step_role() => _diagnostic.Arguments["step"].ShouldEqual(DiagnosticArgument.Keyword("given"));
    [Fact] void should_keep_the_authored_specification_argument() => _diagnostic.Arguments["specification"].ShouldEqual(DiagnosticArgument.Declaration("specification", ["Billing", "Invoices", "RegisterInvoice", "RegisteringAnInvoice"]));
}
