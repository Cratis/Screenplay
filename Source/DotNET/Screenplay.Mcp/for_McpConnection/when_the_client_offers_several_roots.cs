// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_the_client_offers_several_roots : given.a_dynamic_connection
{
    JsonElement _response;

    void Establish() => Initialize(true, RootsAnswer(ModelPath, EmptyPath));

    void Because() => _response = Call("open-workspace");

    [Fact] void should_fail() => Failed(_response).ShouldBeTrue();
    [Fact] void should_list_the_roots_to_choose_from() => Text(_response).ShouldContain("pass open-workspace with a path");
}
