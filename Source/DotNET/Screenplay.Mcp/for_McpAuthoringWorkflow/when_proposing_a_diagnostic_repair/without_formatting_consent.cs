// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_proposing_a_diagnostic_repair;

public class without_formatting_consent : given.a_repairable_workspace
{
    JsonElement _result;

    void Because() => _result = Call("propose-repair", new
    {
        expectedRevision = Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
        diagnosticCode = Repair.GetProperty("diagnosticCode").GetString(),
        subject = Repair.GetProperty("subject")
    });

    [Fact] void should_report_the_distinct_consent_error() => _result.GetProperty("error").GetProperty("message").GetString().ShouldContain("FormattingConsentRequired");
}
