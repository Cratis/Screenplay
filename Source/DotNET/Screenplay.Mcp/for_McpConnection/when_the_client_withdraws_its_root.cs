// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_the_client_withdraws_its_root : given.a_dynamic_connection
{
    JsonElement _response;

    void Establish()
    {
        Initialize(true, RootsAnswer(ModelPath));
        Call("open-workspace");
    }

    void Because()
    {
        Connection.Handle(
            /*lang=json,strict*/ """{"jsonrpc":"2.0","method":"notifications/roots/list_changed"}""",
            new StringReader(RootsAnswer()),
            new StringWriter());
        _response = Call("diagnostics");
    }

    [Fact] void should_unbind_the_workspace() => Tools.ClientDerivedRootPath.ShouldBeNull();
    [Fact] void should_read_the_empty_fallback() => Failed(_response).ShouldBeFalse();
    [Fact] void should_not_create_the_users_screenplay_folder() => Directory.Exists(Path.Combine(DocumentsPath, "Screenplay")).ShouldBeFalse();
}
