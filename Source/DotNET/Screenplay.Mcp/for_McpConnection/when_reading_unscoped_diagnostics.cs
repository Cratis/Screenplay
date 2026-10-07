// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_unscoped_diagnostics : given.a_connection
{
    JsonElement _result;

    void Establish() => Initialize();
    void Because() => _result = Call("diagnostics").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_preserve_the_response_key_set() => _result.EnumerateObject().Select(property => property.Name).ShouldContainOnly("success", "sourceRevision", "fileCount", "summary", "page");
    [Fact] void should_preserve_the_full_application_verdict() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
}
