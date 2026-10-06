// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

using FactAttribute = Cratis.Screenplay.Mcp.for_McpConnection.UnixLinks.FactAttribute;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_a_hardlinked_worktree_pointer : given.a_worktree_connection
{
    string _forged;
    JsonElement _refused;
    void Establish()
    {
        _forged = Path.Combine(RootPath, "forged");
        CreateModel(_forged);
        var start = new ProcessStartInfo("/bin/ln") { UseShellExecute = false, ArgumentList = { Path.Combine(WorktreePath, ".git"), Path.Combine(_forged, ".git") } };
        using var process = Process.Start(start)!;
        process.WaitForExit(10000).ShouldBeTrue();
        process.ExitCode.ShouldEqual(0);
    }
    void Because() => _refused = Call("open-workspace", new { path = _forged }).GetProperty("result");
    [FactAttribute] void should_require_the_registered_checkout_not_just_marker_file_identity() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [FactAttribute] void should_refuse_membership_rather_than_a_missing_model() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("registered worktree of the configured repository");
}
