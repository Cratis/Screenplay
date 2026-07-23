// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_multiline_descriptions : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          description
            ```
            Everything related to invoicing customers.
            Registration and lifecycle of invoices.
            ```

          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                description
                  ```
                  Registers a new invoice.
                  The invoice starts out as a draft.
                  ```
                invoiceId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;
    ModuleSyntax _module;
    CommandSyntax _command;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _module = _result.Value!.Modules.Single();
        _command = _module.Features.Single().Slices.Single().Commands.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_module_description() => _module.Description.ShouldEqual("Everything related to invoicing customers.\nRegistration and lifecycle of invoices.");
    [Fact] void should_have_the_command_description() => _command.Description.ShouldEqual("Registers a new invoice.\nThe invoice starts out as a draft.");
    [Fact] void should_keep_the_command_properties() => _command.Properties.Single().Name.ShouldEqual("invoiceId");
}
