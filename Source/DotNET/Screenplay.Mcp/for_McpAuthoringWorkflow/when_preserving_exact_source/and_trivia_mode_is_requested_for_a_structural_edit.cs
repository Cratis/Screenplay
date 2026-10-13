// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_preserving_exact_source;

public class and_trivia_mode_is_requested_for_a_structural_edit : given.an_exact_source_connection
{
    JsonElement _refused;

    void Because()
    {
        var arguments = Arguments(ReplaceSource(WithImport), CreateNotifications());
        arguments["formatting"] = "PreserveTrivia";
        _refused = Call("propose-source", arguments).GetProperty("result");
    }

    [Fact] void should_still_refuse_the_structural_patch() => _refused.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_the_failure_kind() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("ProposalRejected");
    [Fact] void should_offer_exact_source_preservation() => _refused.GetProperty("structuredContent").GetProperty("conflicts").GetRawText().ShouldContain("PreserveExactSource");
}
