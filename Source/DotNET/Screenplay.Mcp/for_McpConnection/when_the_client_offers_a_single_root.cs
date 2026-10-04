// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_the_client_offers_a_single_root : given.a_dynamic_connection
{
    JsonElement _opened;

    void Establish() => Initialize(true, RootsAnswer(ModelPath));

    void Because() => _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_open_the_clients_root() => _opened.GetProperty("readiness").GetProperty("authoringAccepted").GetBoolean().ShouldBeTrue();
}
