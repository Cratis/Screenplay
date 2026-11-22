// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_the_first_property_has_an_invalid_type : given.catalog_arguments
{
    void Establish()
    {
        _tool = "read-workspace";
        _arguments = JsonDocument.Parse("{\"offset\":\"wrong\",\"view\":\"unsupported\",\"expectedRevision\":\"r\"}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_reject_the_first_property() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_report_the_first_type_before_a_later_enum() => Failure.Message.ShouldEqual("'offset' must be integer.");
    [Fact] void should_report_invalid_arguments() => Failure.Code.ShouldEqual(-32602);
    [Fact] void should_report_the_failure_kind() => Failure.FailureKind.ShouldEqual("InvalidArguments");
}
