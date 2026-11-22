// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_an_integer_is_fractional : given.catalog_arguments
{
    void Establish()
    {
        _tool = "read-document";
        _arguments = JsonDocument.Parse("{\"path\":\"model.play\",\"offset\":1.5}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_reject_the_fraction() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_report_the_exact_message() => Failure.Message.ShouldEqual("'offset' must be integer.");
    [Fact] void should_report_invalid_arguments() => Failure.Code.ShouldEqual(-32602);
    [Fact] void should_report_the_failure_kind() => Failure.FailureKind.ShouldEqual("InvalidArguments");
}
