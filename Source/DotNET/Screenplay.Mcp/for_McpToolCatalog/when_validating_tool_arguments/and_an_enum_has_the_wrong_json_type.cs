// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_an_enum_has_the_wrong_json_type : given.catalog_arguments
{
    void Establish()
    {
        _tool = "dependency-graph";
        _arguments = JsonDocument.Parse("{\"view\":42}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_reject_the_wrong_type() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_report_type_before_enum_membership() => Failure.Message.ShouldEqual("'view' must be string.");
    [Fact] void should_report_invalid_arguments() => Failure.Code.ShouldEqual(-32602);
    [Fact] void should_report_the_failure_kind() => Failure.FailureKind.ShouldEqual("InvalidArguments");
}
