// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_observable_query : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateView InvoiceList
              query ListInvoices => InvoiceListReadModel[]
                observable
                filter status InvoiceStatus?

              query GetInvoice => InvoiceDetailsReadModel
                by invoiceId InvoiceId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_mark_the_observable_query() => Query("ListInvoices").IsObservable.ShouldBeTrue();
    [Fact] void should_keep_the_filter() => Query("ListInvoices").Filters.Single().Name.ShouldEqual("status");
    [Fact] void should_leave_an_unmarked_query_one_shot() => Query("GetInvoice").IsObservable.ShouldBeFalse();

    QuerySyntax Query(string name) =>
        _result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single(_ => _.Name == name);
}
