// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_the_client_answers_roots_list_as_the_schema_defines : given.a_dynamic_connection
{
    JsonElement _described;

    // Written out rather than built from the shared helper, so the wire shape is pinned to the MCP schema
    // (ListRootsResult has a 'roots' array) and not to whatever the server happens to read.
    void Establish() => Initialize(true, $$$"""{"jsonrpc":"2.0","id":"screenplay-roots","result":{"roots":[{"uri":"{{{new Uri(ModelPath).AbsoluteUri}}}","name":"model"}]}}""");

    void Because() => _described = Call("describe-application");

    [Fact] void should_offer_the_root_to_the_tools() => Tools.ClientRoots.ShouldContainOnly([new Uri(ModelPath).AbsoluteUri]);
    [Fact] void should_work_in_the_offered_root_without_open_workspace() => Failed(_described).ShouldBeFalse();
}
