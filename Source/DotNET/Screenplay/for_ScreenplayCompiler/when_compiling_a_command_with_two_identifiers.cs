// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_with_two_identifiers : given.a_compiler
{
    const string Source =
        """
        concept InvoiceId  : Uuid
        concept CustomerId : Uuid

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId  InvoiceId identifier
                customerId CustomerId identifier
        """;

    CompilationResult<ApplicationSyntax> _result;
    CommandSyntax _command;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _command = _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    }

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_second_identifier() => _result.Diagnostics.Single().Message.ShouldEqual("Command 'RegisterInvoice' already marks 'invoiceId' as identifier - only one property can be the identifier");
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_keep_the_first_identifier() => _command.Properties.Single(_ => _.IsIdentifier).Name.ShouldEqual("invoiceId");
}
