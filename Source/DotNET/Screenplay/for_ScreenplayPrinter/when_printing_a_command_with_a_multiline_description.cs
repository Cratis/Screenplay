// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_command_with_a_multiline_description : given.a_printer
{
    const string Source =
        """
        module Invoicing
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

    CompilationResult<ApplicationSyntax> _original;
    string _printed;
    CompilationResult<ApplicationSyntax> _reparsed;
    string _printedAgain;

    void Because()
    {
        _original = _compiler.Compile(Source);
        _printed = _printer.Print(_original.Value!);
        _reparsed = _compiler.Compile(_printed);
        _printedAgain = _printer.Print(_reparsed.Value!);
    }

    [Fact] void should_reparse_successfully() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_print_the_fenced_form() => _printed.Contains("        description\n          ```\n          Registers a new invoice.\n          The invoice starts out as a draft.\n          ```\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_command_description() => Command(_reparsed).Description.ShouldEqual(Command(_original).Description);

    static CommandSyntax Command(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}
