// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_the_user_fallback_without_a_documents_folder : given.a_dynamic_connection
{
    JsonElement _response;

    void Establish()
    {
        Directory.Delete(DocumentsPath);
        Initialize(false);
    }

    void Because() => _response = Call("open-workspace");

    [Fact] void should_open() => Failed(_response).ShouldBeFalse();
    [Fact] void should_open_an_empty_workspace() => _response.GetProperty("result").GetProperty("structuredContent").GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_not_create_the_documents_folder() => Directory.Exists(DocumentsPath).ShouldBeFalse();
}
