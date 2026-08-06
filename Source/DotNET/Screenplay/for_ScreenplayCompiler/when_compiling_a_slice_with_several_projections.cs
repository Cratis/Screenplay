// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_slice_with_several_projections : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView InvoiceList
              query ListInvoices => InvoiceListReadModel[]

              projection InvoiceList => InvoiceListReadModel
                from InvoiceRegistered
                  status = "draft"

              projection InvoicePolicy => InvoicePolicyReadModel
                from InvoiceCancelled
                  cancelled = true
        """;

    CompilationResult<ApplicationSyntax> _result;
    SliceSyntax _slice;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _slice = _result.Value!.Modules.Single().Features.Single().Slices.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_both_projections() => _slice.Projections.Select(_ => _.Name).ShouldContainOnly("InvoiceList", "InvoicePolicy");
    [Fact] void should_keep_them_in_declaration_order() => _slice.Projections.First().Name.ShouldEqual("InvoiceList");
    [Fact] void should_keep_the_read_model_of_the_second() => _slice.Projections.Last().ReadModel.ShouldEqual("InvoicePolicyReadModel");
}
