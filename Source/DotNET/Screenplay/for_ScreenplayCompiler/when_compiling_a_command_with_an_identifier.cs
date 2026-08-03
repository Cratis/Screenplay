// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_with_an_identifier : given.a_compiler
{
    const string Source =
        """
        concept InvoiceId     : Uuid
        concept InvoiceNumber : String

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId     InvoiceId identifier
                invoiceNumber InvoiceNumber
        """;

    CompilationResult<ApplicationSyntax> _result;
    CommandSyntax _command;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _command = _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_mark_the_identifier() => _command.Properties.Single(_ => _.IsIdentifier).Name.ShouldEqual("invoiceId");
    [Fact] void should_keep_the_declared_type() => _command.Properties.First().Type.Name.ShouldEqual("InvoiceId");
    [Fact] void should_leave_the_other_property_unmarked() => _command.Properties.Last().IsIdentifier.ShouldBeFalse();
}
