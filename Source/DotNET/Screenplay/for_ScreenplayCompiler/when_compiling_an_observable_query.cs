// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_observable_query : given.a_compiler
{
    const string Source =
        """
        concept InvoiceId : Uuid

        module Invoicing
          feature Invoices
            slice StateView InvoiceList
              query LiveInvoices => observable InvoiceListReadModel[]
                description "Every invoice, kept current while the board is open"

              query LiveInvoice => observable InvoiceDetailsReadModel?
                by invoiceId InvoiceId

              query ListInvoices => InvoiceListReadModel[]
        """;

    CompilationResult<ApplicationSyntax> _result;
    QuerySyntax _liveCollection;
    QuerySyntax _liveInstance;
    QuerySyntax _oneShot;

    void Because()
    {
        _result = _compiler.Compile(Source);
        var queries = _result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.ToList();
        _liveCollection = queries[0];
        _liveInstance = queries[1];
        _oneShot = queries[2];
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_mark_the_live_collection_query_observable() => _liveCollection.IsObservable.ShouldBeTrue();
    [Fact] void should_keep_the_collection_return_type() => _liveCollection.ReturnType.Name.ShouldEqual("InvoiceListReadModel");
    [Fact] void should_keep_the_collection_suffix() => _liveCollection.ReturnType.IsCollection.ShouldBeTrue();
    [Fact] void should_keep_the_description() => _liveCollection.Description.ShouldEqual("Every invoice, kept current while the board is open");
    [Fact] void should_mark_the_live_instance_query_observable() => _liveInstance.IsObservable.ShouldBeTrue();
    [Fact] void should_keep_the_optional_suffix() => _liveInstance.ReturnType.IsOptional.ShouldBeTrue();
    [Fact] void should_keep_the_identifying_parameter() => _liveInstance.By!.Name.ShouldEqual("invoiceId");
    [Fact] void should_leave_a_one_shot_query_unmarked() => _oneShot.IsObservable.ShouldBeFalse();
}
