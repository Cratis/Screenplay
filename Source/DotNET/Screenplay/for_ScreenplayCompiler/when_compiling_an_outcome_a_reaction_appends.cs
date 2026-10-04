// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_outcome_a_reaction_appends : given.a_compiler
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid identifier
                produces InvoiceRegistered
                  for invoiceId
                  invoiceId = invoiceId
              event InvoiceRegistered
                invoiceId Uuid
              specification RegisteringAnInvoice
                when RegisterInvoice
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then InvoiceRegistered
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then InvoiceWelcomed
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
            slice Automation Welcome
              reaction Welcomer
                when InvoiceRegistered
                  invoiceId
                  produces InvoiceWelcomed
                    invoiceId = invoiceId
              event InvoiceWelcomed
                invoiceId Uuid
        """;

    CompilationResult<ApplicationSyntax> _withReaction;
    CompilationResult<ApplicationSyntax> _withoutReaction;

    void Because()
    {
        _withReaction = _compiler.Compile(Source);
        _withoutReaction = _compiler.Compile(Source[..Source.IndexOf("      reaction Welcomer", StringComparison.Ordinal)] + "      event InvoiceWelcomed\n        invoiceId Uuid\n");
    }

    [Fact] void should_leave_an_event_a_reaction_may_append_to_execution() => _withReaction.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_still_report_it_when_nothing_reacts() => _withoutReaction.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldEqual(1);
}
