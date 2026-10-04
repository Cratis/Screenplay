// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_enforcing_fixed_repair_roots : given.a_connection
{
    [Fact]
    void should_preserve_the_published_failure_constructor()
    {
        typeof(McpFailure).GetConstructor([typeof(string), typeof(int)])!.GetParameters().Length.ShouldEqual(2);
        new McpFailure("legacy", -32602).Message.ShouldEqual("legacy");
    }

    [Fact]
    void should_refuse_another_root_without_losing_the_approved_workspace()
    {
        Initialize();
        _ = Call("open-workspace");
        var child = Path.Combine(RootPath, "other");
        Directory.CreateDirectory(child);
        var refusal = Call("open-workspace", new { path = child }).GetProperty("result");
        refusal.GetProperty("isError").GetBoolean().ShouldBeTrue();
        refusal.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        Call("open-workspace", new { path = RootPath + Path.DirectorySeparatorChar }).GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    void should_keep_dynamic_explicit_root_switching()
    {
        var dynamicWorkspaces = new McpWorkspaces();
        _ = dynamicWorkspaces.Open(System.Text.Json.JsonSerializer.SerializeToElement(new { path = RootPath }));
        var child = Path.Combine(RootPath, "other");
        Directory.CreateDirectory(child);
        _ = dynamicWorkspaces.Open(System.Text.Json.JsonSerializer.SerializeToElement(new { path = child }));
        dynamicWorkspaces.ReadRoot().DirectoryPath.ShouldEqual(child);
    }
}
