// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_tags : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

                produces InvoiceRegistered
                  tag audit
                  invoiceId = invoiceId

              event InvoiceRegistered
                tag invoicing
                tag "billing"
                tag $context.identity.id
                invoiceId Uuid

            slice Translate LegacySync
              capture LegacyCapture
                source api
                  api LegacyApi
                key id
                append InvoiceRegistered
                  tag legacy
                  when added
                    invoiceId = $.id
        """;

    CompilationResult<ApplicationSyntax> _result;
    EventSyntax _event;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _event = _result.Value!.Modules.Single().Features.Single().Slices.First().Events.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_event_tags() => _event.Tags!.Count().ShouldEqual(3);
    [Fact] void should_parse_the_bare_identifier_tag_as_literal() => ((LiteralExpressionSyntax)_event.Tags!.First().Value).Value.ShouldEqual("invoicing");
    [Fact] void should_parse_the_string_literal_tag() => ((LiteralExpressionSyntax)_event.Tags!.ElementAt(1).Value).Value.ShouldEqual("billing");
    [Fact] void should_parse_the_context_expression_tag() => ((ContextExpressionSyntax)_event.Tags!.Last().Value).Path.ShouldEqual("identity.id");
    [Fact] void should_parse_the_produces_tag() => ((LiteralExpressionSyntax)Produces.Tags!.Single().Value).Value.ShouldEqual("audit");
    [Fact] void should_keep_the_produces_mappings() => Produces.Mappings.Count().ShouldEqual(1);
    [Fact] void should_parse_the_capture_append_tag() => ((LiteralExpressionSyntax)Append.Tags!.Single().Value).Value.ShouldEqual("legacy");
    [Fact] void should_keep_the_capture_append_when() => Append.When.ShouldNotBeNull();

    ProducesSyntax Produces => _result.Value!.Modules.Single().Features.Single().Slices.First().Commands.Single().Produces.Single();

    CaptureAppendSyntax Append => _result.Value!.Modules.Single().Features.Single().Slices.Last().Captures.Single().Appends.Single();
}
