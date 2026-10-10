// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_comparing_a_path_outside_the_configured_root : given.a_connection
{
    JsonElement _result;
    string _outside = null!;

    void Establish()
    {
        Initialize();
        _outside = Path.Combine(RootPath, "outside");
        Directory.CreateDirectory(_outside);
    }

    void Because() => _result = Call("semantic-diff", new { before = new { path = _outside }, after = new { path = RootPath } }).GetProperty("result");

    [Fact] void should_refuse_the_outside_path() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_identify_the_admission_failure() => _result.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_not_change_the_outside_folder() => Directory.GetFileSystemEntries(_outside).ShouldBeEmpty();
}
