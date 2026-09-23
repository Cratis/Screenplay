// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpLargeModels;

public class when_source_bytes_change_without_changing_length : for_McpConnection.given.a_connection
{
    McpSnapshot _first = null!;
    McpSnapshot _unchanged = null!;
    McpSnapshot _changed = null!;
    long _beforeLength;
    long _afterLength;

    void Because()
    {
        var cache = new McpSourceCache();
        var path = Path.Combine(RootPath, "application.play");
        _beforeLength = new FileInfo(path).Length;
        _first = cache.Read(Root.Read());
        _unchanged = cache.Read(Root.Read());
        File.WriteAllText(path, Source.Replace("Registers a new project", "Registers a big project", StringComparison.Ordinal));
        _afterLength = new FileInfo(path).Length;
        _changed = cache.Read(Root.Read());
    }

    [Fact] void should_keep_the_file_length_unchanged() => _afterLength.ShouldEqual(_beforeLength);
    [Fact] void should_reuse_the_identical_snapshot() => ReferenceEquals(_first, _unchanged).ShouldBeTrue();
    [Fact] void should_invalidate_on_exact_byte_changes() => ReferenceEquals(_first, _changed).ShouldBeFalse();
    [Fact] void should_change_the_source_revision() => (_first.SourceRevision == _changed.SourceRevision).ShouldBeFalse();
}
