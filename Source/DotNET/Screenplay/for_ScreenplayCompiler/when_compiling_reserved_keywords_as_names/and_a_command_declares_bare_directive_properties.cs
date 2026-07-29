// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_reserved_keywords_as_names;

public class and_a_command_declares_bare_directive_properties : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId   Uuid
                description String
                validate    Bool
                handler     String
                concurrency Int
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
    [Fact] void should_declare_every_property() => _command.Properties.Count().ShouldEqual(5);
    [Fact] void should_declare_the_description_property() => Property("description").Type.Name.ShouldEqual("String");
    [Fact] void should_declare_the_validate_property() => Property("validate").Type.Name.ShouldEqual("Bool");
    [Fact] void should_declare_the_handler_property() => Property("handler").Type.Name.ShouldEqual("String");
    [Fact] void should_declare_the_concurrency_property() => Property("concurrency").Type.Name.ShouldEqual("Int");
    [Fact] void should_not_have_a_description() => _command.Description.ShouldBeNull();
    [Fact] void should_not_have_a_validate_block() => _command.Validations.ShouldBeEmpty();
    [Fact] void should_not_have_a_handler() => _command.Handler.ShouldBeNull();
    [Fact] void should_not_have_a_concurrency_block() => _command.Concurrency.ShouldBeNull();

    PropertySyntax Property(string name) => _command.Properties.Single(_ => _.Name == name);
}
