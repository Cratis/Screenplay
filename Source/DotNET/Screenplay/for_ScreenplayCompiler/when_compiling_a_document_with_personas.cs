// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_personas : given.a_compiler
{
    const string Source =
        """
        policy IsAccountant
          require role "Accountant"

        policy CanManageInvoice
          require role "InvoiceManager"

        persona Accountant
          description "Keeps the books and approves invoices"
          policy IsAccountant
          policy CanManageInvoice

        persona Clerk
          policy CanManageInvoice

        module Invoicing
          feature Invoices
        """;

    CompilationResult<ApplicationSyntax> _result;
    PersonaSyntax _accountant;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _accountant = _result.Value!.Personas!.First();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_both_personas() => _result.Value!.Personas!.Count().ShouldEqual(2);
    [Fact] void should_have_the_accountant_name() => _accountant.Name.ShouldEqual("Accountant");
    [Fact] void should_have_the_accountant_description() => _accountant.Description.ShouldEqual("Keeps the books and approves invoices");
    [Fact] void should_have_the_accountant_policies() => _accountant.Policies.ShouldContainOnly("IsAccountant", "CanManageInvoice");
    [Fact] void should_have_no_description_on_the_clerk() => _result.Value!.Personas!.Last().Description.ShouldBeNull();
}
