// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_the_user_fallback : given.a_dynamic_connection
{
    JsonElement _opened;

    void Establish() => Initialize(false);

    void Because() => _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_open_an_empty_workspace() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_not_create_the_fallback_folder() => Directory.EnumerateFileSystemEntries(DocumentsPath).ShouldBeEmpty();
}
