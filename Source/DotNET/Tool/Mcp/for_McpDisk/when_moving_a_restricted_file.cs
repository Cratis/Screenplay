// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDisk;

public class when_moving_a_restricted_file : given.a_restricted_source
{
    McpDiskResult _result = null!;

    void Because() => _result = new McpDisk(Root).Apply(Rename(Workspace()));

    [Fact] void should_apply_the_move() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_restricted_access() => Access(Path.Combine(RootPath, "renamed.play")).ShouldEqual(OriginalAccess);
    [Fact] void should_preserve_the_source_bytes() => File.ReadAllText(Path.Combine(RootPath, "renamed.play")).ShouldEqual(Source);
}
