// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_reserved_keywords_as_names;

public class and_a_command_keeps_its_directives : given.a_compiler
{
    const string Source =
        """
        policy CanManageInvoice
          require authenticated

        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                description "Registers a new invoice"
                invoiceId Uuid
                authorize CanManageInvoice
                validate
                  invoiceId not empty
                concurrency
                  eventSource
                produces InvoiceRegistered

              event InvoiceRegistered
                invoiceId Uuid
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
    [Fact] void should_have_only_the_declared_property() => _command.Properties.Single().Name.ShouldEqual("invoiceId");
    [Fact] void should_have_the_description() => _command.Description.ShouldEqual("Registers a new invoice");
    [Fact] void should_have_the_authorize_declaration() => _command.Authorize!.Policies.Single().Name.ShouldEqual("CanManageInvoice");
    [Fact] void should_have_the_validate_block() => _command.Validations.Single().ShouldBeOfExactType<DeclarativeValidateSyntax>();
    [Fact] void should_have_the_concurrency_block() => _command.Concurrency!.EventSource.ShouldBeTrue();
    [Fact] void should_have_the_production() => _command.Produces.Single().Event.ShouldEqual("InvoiceRegistered");
}
