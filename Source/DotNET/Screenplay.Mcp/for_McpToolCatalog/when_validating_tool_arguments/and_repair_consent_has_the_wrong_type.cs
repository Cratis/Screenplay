// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_repair_consent_has_the_wrong_type : given.catalog_arguments
{
    void Establish()
    {
        _tool = "propose-repair";
        _arguments = JsonDocument.Parse("{\"expectedRevision\":\"r\",\"expectedCatalogRevision\":\"c\",\"diagnosticCode\":\"PLAY0470\",\"subject\":{},\"formatting\":false}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_reject_present_but_invalid_consent() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_report_the_regular_type_error() => Failure.Message.ShouldEqual("'formatting' must be string.");
    [Fact] void should_report_invalid_arguments() => Failure.Code.ShouldEqual(-32602);
    [Fact] void should_not_report_missing_consent() => Failure.FailureKind.ShouldEqual("InvalidArguments");
}
