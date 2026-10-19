// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_describing_performed_query_shapes : given.a_connection
{
    const string PerformedSource =
        """
        module Billing
          feature Invoices
            slice StateChange RecordInvoice
              event InvoiceRecorded
                invoiceId String
              command RecordInvoice
                invoiceId String identifier
                handler
                  file Handlers/RecordInvoice.cs
            slice StateView InvoiceFigures
              readmodel InvoiceFigures
                invoiceId String
                status String
              query FiguresByStatus => observable InvoiceFigures[]
                filter status String optional
                performer
                  file Queries/FiguresByStatus.cs
        """;

    JsonElement _items;

    JsonElement Performed => _items.EnumerateArray().Single(item => item.GetProperty("role").GetString() == "QueryPerformer");
    JsonElement Handler => _items.EnumerateArray().Single(item => item.GetProperty("role").GetString() == "CommandHandler");

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), PerformedSource);
        Initialize();
        var revision = Call("open-workspace", new { applicationName = "Billing" }).GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString();
        _items = Call("read-workspace", new { expectedRevision = revision, view = "implementation-requirements" }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items");
    }

    [Fact] void should_name_the_returned_read_model() => Performed.GetProperty("queryShape").GetProperty("readModel").GetProperty("name").GetString().ShouldEqual("InvoiceFigures");
    [Fact] void should_identify_the_returned_read_model() => Performed.GetProperty("queryShape").GetProperty("readModel").GetProperty("semanticId").GetString().ShouldNotBeNull();
    [Fact] void should_address_the_returned_read_model() => Performed.GetProperty("queryShape").GetProperty("readModel").GetProperty("address").GetProperty("kind").GetString().ShouldEqual("ReadModel");
    [Fact] void should_report_the_cardinality() => Performed.GetProperty("queryShape").GetProperty("cardinality").GetString().ShouldEqual("Many");
    [Fact] void should_report_live_delivery() => Performed.GetProperty("queryShape").GetProperty("delivery").GetString().ShouldEqual("Live");
    [Fact] void should_name_the_filter() => Performed.GetProperty("queryShape").GetProperty("arguments")[0].GetProperty("name").GetString().ShouldEqual("status");
    [Fact] void should_report_the_filter_kind() => Performed.GetProperty("queryShape").GetProperty("arguments")[0].GetProperty("kind").GetString().ShouldEqual("Filter");
    [Fact] void should_report_the_filter_type() => Performed.GetProperty("queryShape").GetProperty("arguments")[0].GetProperty("type").GetProperty("primitive").GetString().ShouldEqual("Text");
    [Fact] void should_report_the_filter_as_optional() => Performed.GetProperty("queryShape").GetProperty("arguments")[0].GetProperty("optional").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_no_shape_for_other_roles() => Handler.GetProperty("queryShape").ValueKind.ShouldEqual(JsonValueKind.Null);
}
