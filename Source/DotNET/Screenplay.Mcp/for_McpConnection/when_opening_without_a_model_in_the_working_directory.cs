// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_without_a_model_in_the_working_directory : given.a_dynamic_connection
{
    JsonElement _response;

    void Establish() => Initialize(false);

    void Because() => _response = Call("open-workspace");

    [Fact] void should_fail() => Failed(_response).ShouldBeTrue();
    [Fact] void should_tell_the_caller_to_pass_a_path() => Text(_response).ShouldContain("Pass open-workspace with a path");
}
