// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// A specification runs only its own slice, so only a reaction its slice declares can make the invocation it states.
public class when_compiling_an_invocation_no_reaction_of_the_slice_makes : given.a_compiler
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateChange CloseInvoice
              command CloseInvoice
                invoiceId Uuid identifier
                produces InvoiceClosed
                  for invoiceId
                  invoiceId = invoiceId
              event InvoiceClosed
                invoiceId Uuid
            slice Automation Closing
              event InvoicePaid
                invoiceId Uuid
              specification ClosingAPaidInvoice
                when append InvoicePaid
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then invokes CloseInvoice
            slice Automation Closer
              reaction Closer
                when InvoicePaid
                  invoiceId
                  invokes CloseInvoice
                    invoiceId = invoiceId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_it_although_another_slice_invokes_it() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationInvocation).Message
        .ShouldEqual("Specification 'ClosingAPaidInvoice' states 'then invokes CloseInvoice', but no reaction of its slice 'Closing' invokes 'CloseInvoice'");
    [Fact] void should_report_it_on_the_statement() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationInvocation).Location.Line.ShouldEqual(17);
}
