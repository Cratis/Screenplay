// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_reserved_keywords_as_names;

public class and_an_event_declares_a_tag_property : given.a_compiler
{
    const string Source =
        """
        concept TagType : String

        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              event InvoiceRegistered
                tag       audit
                tag       TagType
                @tag      TagType
                invoiceId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;
    EventSyntax _event;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _event = _result.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_that_the_tag_line_is_not_a_property() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_point_at_the_ambiguous_line() => _result.Diagnostics.Single().Location.Line.ShouldEqual(8);
    [Fact] void should_keep_both_tags() => _event.Tags!.Count().ShouldEqual(2);
    [Fact] void should_declare_the_escaped_property() => _event.Properties.Single(_ => _.Name == "tag").Type.Name.ShouldEqual("TagType");
    [Fact] void should_declare_the_other_property() => _event.Properties.Single(_ => _.Name == "invoiceId").Type.Name.ShouldEqual("Uuid");
    [Fact] void should_not_warn_about_a_lowercase_tag_value() => _result.Diagnostics.Count().ShouldEqual(1);
}
