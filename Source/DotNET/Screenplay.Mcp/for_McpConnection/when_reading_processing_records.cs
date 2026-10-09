// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_processing_records : given.a_connection
{
    JsonElement _response;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            purpose Billing
              basis contract
            purpose Ledger
              basis legalObligation
              erasure exception legalObligation
            concept Name : String pii
            module M
              purpose Billing
              purpose Ledger
              feature F
                slice StateChange S
                  command Record
                    name Name
            """);
        Initialize();
    }

    void Because() => _response = Call("processing-record", new { controllerName = "Controller", controllerContact = "contact@example.test", limit = 1 }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_include_the_notice() => _response.GetProperty("notice").GetString().ShouldEqual("Generated from declarations in this model. Not legal advice.");
    [Fact] void should_include_supplied_controller_details() => _response.GetProperty("controllerName").GetString().ShouldEqual("Controller");
    [Fact] void should_page_purposes() => _response.GetProperty("rows").GetProperty("totalCount").GetInt32().ShouldEqual(2);
    [Fact] void should_bound_the_first_page() => _response.GetProperty("rows").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_bind_the_page_to_the_source_revision() => _response.GetProperty("rows").GetProperty("revision").GetString().ShouldEqual(_response.GetProperty("sourceRevision").GetString());

    [Fact]
    void should_continue_at_the_pinned_revision()
    {
        var page = Call("processing-record", new { offset = 1, expectedSourceRevision = _response.GetProperty("sourceRevision").GetString() }).GetProperty("result").GetProperty("structuredContent").GetProperty("rows");
        page.GetProperty("items")[0].GetProperty("purpose").GetString().ShouldEqual("Ledger");
        page.GetProperty("items")[0].GetProperty("findings").GetArrayLength().ShouldEqual(1);
    }

    [Fact]
    void should_refuse_a_stale_revision()
    {
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// Changed");
        Call("processing-record", new { offset = 1, expectedSourceRevision = _response.GetProperty("sourceRevision").GetString() }).GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_refuse_invalid_source_without_a_partial_inventory()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "purpose P\n  basis invalid");
        Call("processing-record").GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    }
}
