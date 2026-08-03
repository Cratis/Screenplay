// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_query_with_a_performer : given.a_compiler
{
    const string Source =
        """
        concept TenantId      : Uuid
        concept InvoiceId     : Uuid
        concept InvoiceStatus : String

        module Invoicing
          feature Invoices
            slice StateView InvoiceList
              query ListInvoices => InvoiceListReadModel[]
                description "Every invoice the caller may see"
                filter status   InvoiceStatus?
                filter tenantId TenantId from $context.tenant
                performer
                  sql
                    ```
                    select * from Invoices where TenantId = @tenantId
                    ```

              query GetInvoice => InvoiceDetailsReadModel
                by invoiceId InvoiceId
                performer
                  file Queries/InvoiceDetailsPerformer.cs
        """;

    CompilationResult<ApplicationSyntax> _result;
    QuerySyntax _list;
    QuerySyntax _details;

    void Because()
    {
        _result = _compiler.Compile(Source);
        var queries = _result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.ToList();
        _list = queries[0];
        _details = queries[1];
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_description() => _list.Description.ShouldEqual("Every invoice the caller may see");
    [Fact] void should_leave_a_caller_supplied_parameter_without_a_source() => _list.Filters.First().Source.ShouldBeNull();
    [Fact] void should_source_the_tenant_from_the_context() => ((ContextExpressionSyntax)_list.Filters.Last().Source!).Path.ShouldEqual("tenant");
    [Fact] void should_keep_the_parameter_type() => _list.Filters.Last().Type.Name.ShouldEqual("TenantId");
    [Fact] void should_parse_the_sql_performer() => _list.Performer!.Code!.Language.ShouldEqual("sql");
    [Fact] void should_keep_the_sql() => _list.Performer!.Code!.Code.ShouldEqual("select * from Invoices where TenantId = @tenantId");
    [Fact] void should_parse_the_file_performer() => _details.Performer!.File!.Path.ShouldEqual("Queries/InvoiceDetailsPerformer.cs");
    [Fact] void should_keep_the_identifying_parameter() => _details.By!.Name.ShouldEqual("invoiceId");
}
