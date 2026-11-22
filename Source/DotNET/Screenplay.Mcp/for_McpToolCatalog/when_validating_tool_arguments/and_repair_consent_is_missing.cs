// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.when_validating_tool_arguments;

public class and_repair_consent_is_missing : given.catalog_arguments
{
    void Establish()
    {
        _tool = "propose-repair";
        _arguments = JsonDocument.Parse("{\"extra\":true}");
    }

    void Because() => _error = Catch.Exception(() => McpToolCatalog.Validate(_tool, _arguments.RootElement, _visual));

    [Fact] void should_reject_missing_consent() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_report_consent_before_missing_required_or_unknown_arguments() => Failure.Message.ShouldEqual("FormattingConsentRequired: diagnostic repairs require explicit formatting consent; use the discovered requiredFormatting.");
    [Fact] void should_report_invalid_arguments() => Failure.Code.ShouldEqual(-32602);
    [Fact] void should_report_the_distinct_failure_kind() => Failure.FailureKind.ShouldEqual("FormattingConsentRequired");
}
