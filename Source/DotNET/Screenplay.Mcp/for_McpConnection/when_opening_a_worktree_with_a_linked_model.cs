// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

using FactAttribute = Cratis.Screenplay.Mcp.for_McpConnection.UnixLinks.FactAttribute;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_a_worktree_with_a_linked_model : given.a_worktree_connection
{
    JsonElement _refused;
    void Establish()
    {
        Directory.Move(WorktreeModelPath, WorktreeModelPath + "-original");
        Directory.CreateSymbolicLink(WorktreeModelPath, ModelPath);
    }
    void Because() => _refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
    [FactAttribute] void should_refuse_a_model_link_even_inside_a_registered_checkout() => _refused.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [FactAttribute] void should_report_root_change_refusal() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [FactAttribute] void should_explain_the_link_refusal() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("Symbolic links and reparse points are not admitted");
}
