// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_proposing_a_diagnostic_repair;

public class and_the_subject_is_stale : given.a_repairable_workspace
{
    JsonElement _result;

    void Because() => _result = Call("propose-repair", Arguments(subject: new
    {
        revision = "wsrev1:" + new string('0', 64),
        documentId = Repair.GetProperty("subject").GetProperty("documentId").GetString(),
        path = Repair.GetProperty("subject").GetProperty("path").GetString()
    }));

    [Fact] void should_report_an_unknown_repair() => _result.GetProperty("error").GetProperty("message").GetString().ShouldContain("UnknownRepair");
}
