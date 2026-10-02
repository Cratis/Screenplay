// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class both_a_proposal_and_a_sketch : given.a_host_that_renders_views
{
    JsonElement _response;

    void Because() => _response = Call("visualize-model", new { proposalId = "any", sketch = new[] { new { path = "application.play", source = Source } } });

    [Fact] void should_reject_the_request() => _response.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
}
