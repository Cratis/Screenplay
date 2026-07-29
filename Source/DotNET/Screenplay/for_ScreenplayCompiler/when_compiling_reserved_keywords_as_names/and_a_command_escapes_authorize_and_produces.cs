// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_reserved_keywords_as_names;

public class and_a_command_escapes_authorize_and_produces : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId  Uuid
                @authorize AuthorizationCode
                @produces  ProductionLine
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
    [Fact] void should_declare_every_property() => _command.Properties.Count().ShouldEqual(3);
    [Fact] void should_strip_the_escape_from_the_authorize_property() => Property("authorize").Type.Name.ShouldEqual("AuthorizationCode");
    [Fact] void should_strip_the_escape_from_the_produces_property() => Property("produces").Type.Name.ShouldEqual("ProductionLine");
    [Fact] void should_not_have_an_authorize_declaration() => _command.Authorize.ShouldBeNull();
    [Fact] void should_not_produce_any_event() => _command.Produces.ShouldBeEmpty();

    PropertySyntax Property(string name) => _command.Properties.Single(_ => _.Name == name);
}
