// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_query_with_a_performer : given.a_printer
{
    const string Source =
        """
        concept TenantId      : Uuid
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
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_description() => _roundtrip.Printed.ShouldContain("description \"Every invoice the caller may see\"");
    [Fact] void should_print_the_context_source() => _roundtrip.Printed.ShouldContain("filter tenantId TenantId from $context.tenant");
    [Fact] void should_print_the_performer() => _roundtrip.Printed.ShouldContain("performer");
    [Fact] void should_print_the_sql_tag() => _roundtrip.Printed.ShouldContain("sql");
    [Fact] void should_preserve_the_description() => Query.Description.ShouldEqual("Every invoice the caller may see");
    [Fact] void should_preserve_the_performer_language() => Query.Performer!.Code!.Language.ShouldEqual("sql");
    [Fact] void should_preserve_the_sql() => Query.Performer!.Code!.Code.ShouldEqual("select * from Invoices where TenantId = @tenantId");

    QuerySyntax Query => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single();
}
