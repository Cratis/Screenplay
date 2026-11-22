// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_required_arguments_are_missing : given.catalog_arguments
{
    void Establish()
    {
        _tool = "read-document";
        _arguments = JsonDocument.Parse("{}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_reject_the_missing_argument() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_report_the_exact_message() => Failure.Message.ShouldEqual("'path' is required.");
    [Fact] void should_report_invalid_arguments() => Failure.Code.ShouldEqual(-32602);
    [Fact] void should_report_the_failure_kind() => Failure.FailureKind.ShouldEqual("InvalidArguments");
}
