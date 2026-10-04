// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_from_the_working_directory : given.a_dynamic_connection
{
    JsonElement _opened;

    void Establish()
    {
        Tools.CurrentDirectoryHint = ModelPath;
        Initialize(false);
    }

    void Because() => _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_open_the_model_in_the_working_directory() => _opened.GetProperty("readiness").GetProperty("authoringAccepted").GetBoolean().ShouldBeTrue();
}
