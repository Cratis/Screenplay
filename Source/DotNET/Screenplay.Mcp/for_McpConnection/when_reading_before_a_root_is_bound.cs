// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_before_a_root_is_bound : given.a_dynamic_connection
{
    JsonElement _response;

    void Establish() => Initialize(false);

    void Because() => _response = Call("describe-application");

    [Fact] void should_fail() => Failed(_response).ShouldBeTrue();
    [Fact] void should_ask_for_a_root() => Text(_response).ShouldContain("No Screenplay root is bound");
}
