// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_arguments_are_not_an_object : given.catalog_arguments
{
    void Establish()
    {
        _tool = "propose-repair";
        _arguments = JsonDocument.Parse("[]");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_reject_the_argument_shape() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_report_the_shape_before_missing_consent() => Failure.Message.ShouldEqual("Arguments must be an object.");
    [Fact] void should_report_invalid_arguments() => Failure.Code.ShouldEqual(-32602);
    [Fact] void should_report_the_failure_kind() => Failure.FailureKind.ShouldEqual("InvalidArguments");
}
