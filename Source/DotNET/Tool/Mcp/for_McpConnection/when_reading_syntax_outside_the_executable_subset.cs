// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_reading_syntax_outside_the_executable_subset : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Billing
              feature Invoices
                slice StateChange Create
                  command CreateInvoice
                    amount Decimal
                slice StateView List
                  screen InvoiceList
                    action CreateInvoice label "Create"
              form CreateInvoiceForm for CreateInvoice
                field amount label "Amount"
            """);
        Initialize();
    }

    void Because() => _result = Call("find-declaration", new { name = "CreateInvoiceForm", kind = "Form" }).GetProperty("result");

    [Fact] void should_succeed_without_semantic_binding() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_return_the_concrete_form_field() => _result.GetProperty("structuredContent").GetProperty("matches")[0].GetProperty("syntax").GetProperty("fields")[0].GetProperty("label").GetString().ShouldEqual("Amount");
    [Fact] void should_return_the_original_path() => _result.GetProperty("structuredContent").GetProperty("matches")[0].GetProperty("location").GetProperty("path").GetString().ShouldEqual("application.play");
}
