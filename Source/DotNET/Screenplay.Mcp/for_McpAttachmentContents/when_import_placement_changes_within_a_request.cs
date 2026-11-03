// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAttachmentContents;

public class when_import_placement_changes_within_a_request : given.an_attachment_request
{
    ImmutableArray<WorkspaceDocument> _documents;
    ImmutableArray<FileReferenceSyntax> _first;
    ImmutableArray<FileReferenceSyntax> _changed;

    void Establish()
    {
        _documents =
        [
            Document("application.play", "module Projects\n  import \"feature.play\""),
            Document("feature.play", "feature Registration\n  import \"slice.play\""),
            Document("slice.play", "slice StateChange Register\n  command Register\n    handler\n      file Handler.cs")
        ];
        _first = Request.References(_documents);
    }

    void Because() => _changed = Request.References([Document("application.play", "module Projects\n  feature Registration\n    import \"slice.play\""), .. _documents.Skip(1)]);

    [Fact] void should_discover_the_original_placed_reference() => _first.Single().Path.ShouldEqual("Handler.cs");
    [Fact] void should_rediscover_when_only_import_placement_changes() => _changed.Equals(_first).ShouldBeFalse();
    [Fact] void should_discover_the_new_placed_reference() => _changed.Any(reference => reference.Path == "Handler.cs").ShouldBeTrue();
    [Fact] void should_reuse_the_original_only_for_its_original_complete_snapshot() => Request.References(_documents).Equals(_first).ShouldBeTrue();
}
