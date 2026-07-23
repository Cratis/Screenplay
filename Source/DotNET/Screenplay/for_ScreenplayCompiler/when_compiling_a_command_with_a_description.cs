// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_with_a_description : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                description "Registers a new invoice"
                invoiceId Uuid
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
    [Fact] void should_have_the_command_description() => _command.Description.ShouldEqual("Registers a new invoice");
    [Fact] void should_keep_the_command_properties() => _command.Properties.Single().Name.ShouldEqual("invoiceId");
}
