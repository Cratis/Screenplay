// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_an_explicit_path : given.a_dynamic_connection
{
    JsonElement _opened;

    void Establish() => Initialize(false);

    void Because() => _opened = Call("open-workspace", new { path = ModelPath }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_open_the_model_at_the_path() => _opened.GetProperty("readiness").GetProperty("authoringAccepted").GetBoolean().ShouldBeTrue();
}
