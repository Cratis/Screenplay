// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_an_unregistered_worktree_pointer : given.a_worktree_connection
{
    string _forged;
    JsonElement _refused;
    void Establish()
    {
        _forged = Path.Combine(RootPath, "forged");
        CreateModel(_forged);
        File.Copy(Path.Combine(WorktreePath, ".git"), Path.Combine(_forged, ".git"));
    }
    void Because() => _refused = Call("open-workspace", new { path = _forged }).GetProperty("result");
    [Fact] void should_require_a_matching_registration_back_pointer() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_refuse_membership_rather_than_a_missing_model() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("registered worktree of the configured repository");
}
