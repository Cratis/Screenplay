// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_without_opening_a_workspace : given.a_dynamic_connection
{
    System.Text.Json.JsonElement _response;

    void Establish()
    {
        Tools.CurrentDirectoryHint = ModelPath;
        Initialize(false);
    }

    void Because() => _response = Call("describe-application");

    [Fact] void should_answer_for_the_working_directory() => Failed(_response).ShouldBeFalse();
}
