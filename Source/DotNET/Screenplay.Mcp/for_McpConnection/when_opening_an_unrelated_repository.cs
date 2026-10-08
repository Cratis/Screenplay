// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_an_unrelated_repository : given.a_worktree_connection
{
    JsonElement _refused;
    string _unrelated;
    void Establish()
    {
        _ = Open();
        _unrelated = Path.Combine(RootPath, "unrelated");
        CreateModel(_unrelated);
        Git(_unrelated, "init");
    }
    void Because() => _refused = Call("open-workspace", new { path = _unrelated }).GetProperty("result");
    [Fact] void should_refuse_the_root() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_refuse_membership_rather_than_a_missing_model() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("registered worktree of the configured repository");
}
