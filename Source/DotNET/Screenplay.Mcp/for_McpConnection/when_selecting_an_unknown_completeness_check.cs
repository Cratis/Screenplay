// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_selecting_an_unknown_completeness_check : given.a_connection
{
    JsonElement _error;

    void Establish() => Initialize();
    void Because() => _error = Call("diagnostics", new { checks = "unknown" }).GetProperty("error");

    [Fact] void should_reject_invalid_parameters() => _error.GetProperty("code").GetInt32().ShouldEqual(-32602);
}
