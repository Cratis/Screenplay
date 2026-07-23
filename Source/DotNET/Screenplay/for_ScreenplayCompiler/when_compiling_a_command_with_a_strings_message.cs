// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_with_a_strings_message : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange CancelInvoice
              command CancelInvoice
                invoiceId Uuid
                reason    String

                validate
                  reason not empty  message $strings.invoices.validation.reasonRequired
                  reason max 500    message "Reason must be 500 characters or fewer"

                produces InvoiceCancelled
                  invoiceId = invoiceId

              event InvoiceCancelled
                invoiceId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;
    List<ValidationRuleSyntax> _rules;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _rules = [.. _result.Value!.Modules.Single().Features.Single().Slices.Single()
            .Commands.Single().Validations.OfType<DeclarativeValidateSyntax>().Single().Rules];
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_store_the_strings_reference_as_the_message() => _rules[0].Message.ShouldEqual("$strings.invoices.validation.reasonRequired");
    [Fact] void should_keep_quoted_messages_as_plain_text() => _rules[1].Message.ShouldEqual("Reason must be 500 characters or fewer");
}
