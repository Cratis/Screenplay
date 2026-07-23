// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_descriptions : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          description "Everything related to invoicing customers"

          feature InvoiceManagement
            description "Registering and managing the lifecycle of invoices"

            slice StateChange RegisterInvoice
              description "Registers a new invoice"

              command RegisterInvoice
                invoiceId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;
    ModuleSyntax _module;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _module = _result.Value!.Modules.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_module_description() => _module.Description.ShouldEqual("Everything related to invoicing customers");
    [Fact] void should_have_the_feature_description() => _module.Features.Single().Description.ShouldEqual("Registering and managing the lifecycle of invoices");
    [Fact] void should_have_the_slice_description() => _module.Features.Single().Slices.Single().Description.ShouldEqual("Registers a new invoice");
}
