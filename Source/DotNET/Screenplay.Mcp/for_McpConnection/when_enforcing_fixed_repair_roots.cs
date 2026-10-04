// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

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
    void should_accept_case_aliases_only_when_the_file_system_proves_the_same_directory()
    {
        var path = Path.Combine(RootPath, "ApprovedRoot");
        var alias = Path.Combine(RootPath, "APPROVEDROOT");
        Directory.CreateDirectory(path);
        var approved = new McpRoot(path);
        var workspaces = new McpWorkspaces(approved);
        _ = workspaces.Open(JsonSerializer.SerializeToElement(new { }));
        if (Directory.Exists(alias))
        {
            // A case-insensitive volume resolves the alias to the already existing physical directory.
            McpDirectoryIdentity.Same(approved, new(alias)).ShouldBeTrue();
            _ = workspaces.Open(JsonSerializer.SerializeToElement(new { path = alias }));
        }
        else
        {
            // A case-sensitive volume holds two distinct directories. Similar spelling grants no authority.
            Directory.CreateDirectory(alias);
            McpDirectoryIdentity.Same(approved, new(alias)).ShouldBeFalse();
            var failure = Catch.Exception(() => workspaces.Open(JsonSerializer.SerializeToElement(new { path = alias })));
            (failure as McpFailure)!.FailureKind.ShouldEqual("RootChangeRefused");
        }

        ReferenceEquals(workspaces.ReadRoot(), approved).ShouldBeTrue();
        workspaces.ReadRoot().DirectoryPath.ShouldEqual(path);
    }

    [Fact]
    void should_retain_the_original_root_for_normalized_aliases_but_refuse_traversal_to_another_directory()
    {
        var approved = new McpRoot(RootPath);
        var workspaces = new McpWorkspaces(approved);
        _ = workspaces.Open(JsonSerializer.SerializeToElement(new { path = Path.Combine(RootPath, ".") }));
        var child = Path.Combine(RootPath, "other");
        Directory.CreateDirectory(child);
        _ = workspaces.Open(JsonSerializer.SerializeToElement(new { path = Path.Combine(child, "..") }));
        ReferenceEquals(workspaces.ReadRoot(), approved).ShouldBeTrue();
        var failure = Catch.Exception(() => workspaces.Open(JsonSerializer.SerializeToElement(new { path = Path.Combine(child, "..", "other") })));
        (failure as McpFailure)!.FailureKind.ShouldEqual("RootChangeRefused");
        ReferenceEquals(workspaces.ReadRoot(), approved).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_symbolic_link_even_when_it_points_to_the_approved_directory()
    {
        if (OperatingSystem.IsWindows()) return; // Windows link creation requires privileges not assumed by specs.
        var path = Path.Combine(RootPath, "approved");
        var alias = Path.Combine(RootPath, "alias");
        Directory.CreateDirectory(path);
        Directory.CreateSymbolicLink(alias, path);
        var approved = new McpRoot(path);
        var workspaces = new McpWorkspaces(approved);
        var failure = Catch.Exception(() => workspaces.Open(JsonSerializer.SerializeToElement(new { path = alias })));
        failure.ShouldBeOfExactType<McpFailure>();
        failure.Message.ShouldContain("Symbolic links and reparse points are not admitted");
        ReferenceEquals(workspaces.ReadRoot(), approved).ShouldBeTrue();
        Directory.Delete(alias);
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
