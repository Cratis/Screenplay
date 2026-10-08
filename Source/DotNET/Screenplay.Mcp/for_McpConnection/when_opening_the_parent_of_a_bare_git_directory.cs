// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_the_parent_of_a_bare_git_directory : given.a_worktree_connection
{
    string _parent;
    string _linked;
    JsonElement _refused;
    JsonElement _opened;

    void Establish()
    {
        _parent = Path.Combine(RootPath, "bare-parent");
        _linked = Path.Combine(RootPath, "bare-linked");
        CreateModel(_parent);
        Git(_parent, "clone", "--bare", RepositoryPath, ".git");
        Git(Path.Combine(_parent, ".git"), "worktree", "add", "--detach", _linked, "HEAD");
        var configured = Path.Combine(RootPath, "bare-configured");
        Git(Path.Combine(_parent, ".git"), "worktree", "add", "--detach", configured, "HEAD");
        Connection = new(new McpTools(new McpRoot(Path.Combine(configured, ".cratis", "screenplay"))));
        Initialize();
    }

    void Because()
    {
        _refused = Call("open-workspace", new { path = _parent }).GetProperty("result");
        _opened = Open(_linked);
    }

    [Fact] void should_refuse_the_non_checkout_parent() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_explain_the_membership_refusal() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("registered worktree of the configured repository");
    [Fact] void should_still_open_the_linked_worktree() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(1);
}
