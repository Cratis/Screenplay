// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_proposing_a_diagnostic_repair;

public class and_the_revision_is_stale : given.a_repairable_workspace
{
    JsonElement _result;

    void Because() => _result = Call("propose-repair", Arguments(revision: "wsrev1:" + new string('0', 64)));

    [Fact] void should_return_an_error_verdict() => _result.GetProperty("result").GetProperty("structuredContent").GetProperty("success").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_a_typed_stale_conflict() => _result.GetProperty("result").GetProperty("structuredContent").GetProperty("conflicts").EnumerateArray().Single().GetProperty("kind").GetString().ShouldEqual("StaleWorkspaceRevision");
}
