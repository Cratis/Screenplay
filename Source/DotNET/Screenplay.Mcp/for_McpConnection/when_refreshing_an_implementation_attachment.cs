// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_refreshing_an_implementation_attachment : given.a_connection
{
    string _first = null!;
    string _second = null!;
    string _revision = null!;

    void Because()
    {
        var sourcePath = Path.Combine(RootPath, "application.play");
        File.WriteAllText(sourcePath, "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file Handler.cs");
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "first");
        Initialize();
        var response = Call("open-workspace");
        var opened = response.GetProperty("result").GetProperty("structuredContent");
        _revision = opened.TryGetProperty("revision", out var revision) ? revision.GetString()! : throw new InvalidOperationException(opened.ToString());
        _first = Hash();
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "second");
        _second = Hash();
    }

    [Fact] void should_hash_the_physical_attachment() => _first.ShouldNotBeEmpty();
    [Fact] void should_refresh_when_only_the_attachment_changes() => _first.ShouldNotEqual(_second);
    [Fact] void should_not_change_the_workspace_revision() => Call("read-workspace", new { expectedRevision = _revision, view = "implementation-requirements" }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("revision").GetString().ShouldEqual(_revision);

    string Hash()
    {
        var page = Call("read-workspace", new { expectedRevision = _revision, view = "implementation-requirements" })
            .GetProperty("result").GetProperty("structuredContent").GetProperty("page");
        return page.GetProperty("items").EnumerateArray().Single().GetProperty("contentHash").GetString()!;
    }
}
