// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_a_dialog_template : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          dialog template RegisterInvoiceDialog
            body
            actions

          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

              screen RegisterInvoiceScreen
                template RegisterInvoiceDialog
                  body
                    title "Register invoice"
                  actions
                    action RegisterInvoice
        """;

    CompilationResult<ApplicationSyntax> _result;
    DialogTemplateSyntax _template;
    ScreenTemplateReferenceSyntax _reference;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _template = _result.Value!.Modules.Single().DialogTemplates!.Single();
        _reference = _result.Value!.Modules.Single().Features.Single().Slices.Single()
            .Screens.Single().Directives.OfType<ScreenTemplateReferenceSyntax>().Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_dialog_template_name() => _template.Name.ShouldEqual("RegisterInvoiceDialog");
    [Fact] void should_parse_its_slots() => _template.Slots.Select(slot => slot.Name).ShouldContainOnly("body", "actions");
    [Fact] void should_not_declare_a_screen_template() => _result.Value!.Modules.Single().ScreenTemplates.ShouldBeEmpty();
    [Fact] void should_reference_the_dialog_template_from_the_screen() => _reference.Name.ShouldEqual("RegisterInvoiceDialog");
    [Fact] void should_fill_both_of_its_slots() => _reference.Slots.Select(slot => slot.Name).ShouldContainOnly("body", "actions");
    [Fact] void should_put_the_action_in_the_actions_slot() => _reference.Slots.Single(slot => slot.Name == "actions").Directives.OfType<ScreenActionSyntax>().Single().Command.ShouldEqual("RegisterInvoice");
}
