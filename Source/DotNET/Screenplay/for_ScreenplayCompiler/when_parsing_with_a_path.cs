// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_with_a_path : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange Register
              command Register
                produces InvoiceRegistered
        """;

    CompilationResult<ApplicationSyntax> _parsed;
    CompilationResult<ApplicationSyntax> _compiled;

    void Because()
    {
        _parsed = _compiler.Parse(Source, Path.Combine("Invoicing", "Register.play"));
        _compiled = _compiler.Compile(Source);
    }

    [Fact] void should_succeed() => _parsed.Success.ShouldBeTrue();
    [Fact] void should_leave_cross_references_unresolved() => _parsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_resolve_them_when_compiling_instead() => _compiled.Diagnostics.Single().Message.ShouldEqual("Unknown event 'InvoiceRegistered' - declare it with 'event InvoiceRegistered'");
    [Fact] void should_attribute_the_document_to_the_path() => _parsed.Value!.Location.Path.ShouldEqual(Path.Combine("Invoicing", "Register.play"));
    [Fact] void should_attribute_every_node_to_the_path() => _parsed.Value!.Modules.Single().Features.Single().Slices.Single().Location.Path.ShouldEqual(Path.Combine("Invoicing", "Register.play"));
    [Fact] void should_leave_a_document_with_no_path_unattributed() => _compiled.Value!.Modules.Single().Location.Path.ShouldBeNull();
}
