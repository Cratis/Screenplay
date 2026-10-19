// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_discovering_refactorings;

public class and_the_revision_is_stale : given.a_refactoring_workspace
{
    JsonElement _response;

    void Because()
    {
        var offer = Offers("extract-inline-event")[0];
        Apply(OpenedWorkspace, ProposeExtraction(offer.GetProperty("subject")).GetProperty("result").GetProperty("structuredContent"));
        _response = Call("read-workspace", new { expectedRevision = CurrentRevision, view = "refactorings" });
    }

    [Fact] void should_refuse_the_staleCurrentRevision() => FailureKind(_response).ShouldEqual("StaleRevision");

    static string FailureKind(JsonElement response) => response.TryGetProperty("error", out var error)
        ? error.GetProperty("data").GetProperty("failureKind").GetString()!
        : response.GetProperty("result").GetProperty("structuredContent").GetProperty("failureKind").GetString()!;
}
