// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_screen_with_strings_operands : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateView InvoiceList
              screen InvoiceList
                title $strings.invoices.title
                action RegisterInvoice
                  label $strings.invoices.actions.newInvoice
                table InvoiceListReadModel
                  column invoiceNumber label $strings.invoices.columns.number
                  column status
                summary InvoiceListReadModel
                  field status label $strings.invoices.fields.status
        """;

    CompilationResult<ApplicationSyntax> _result;
    List<ScreenDirectiveSyntax> _directives;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _directives = [.. _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives];
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_store_the_strings_reference_as_the_title() => _directives.OfType<ScreenTitleSyntax>().Single().Text.ShouldEqual("$strings.invoices.title");
    [Fact] void should_store_the_strings_reference_as_the_action_label() => _directives.OfType<ScreenActionSyntax>().Single().Label.ShouldEqual("$strings.invoices.actions.newInvoice");
    [Fact] void should_store_the_strings_reference_as_the_column_label() => _directives.OfType<ScreenTableSyntax>().Single().Columns.First().Label.ShouldEqual("$strings.invoices.columns.number");
    [Fact] void should_keep_columns_without_labels_unlabeled() => _directives.OfType<ScreenTableSyntax>().Single().Columns.Last().Label.ShouldBeNull();
    [Fact] void should_store_the_strings_reference_as_the_field_label() => _directives.OfType<ScreenSummarySyntax>().Single().Fields.Single().Label.ShouldEqual("$strings.invoices.fields.status");
}
