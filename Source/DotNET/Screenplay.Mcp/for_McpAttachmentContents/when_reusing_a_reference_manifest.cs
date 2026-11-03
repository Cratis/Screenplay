// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAttachmentContents;

public class when_reusing_a_reference_manifest : given.an_attachment_request
{
    ImmutableArray<FileReferenceSyntax> _first;

    void Because() => _first = Request.References(Documents);

    [Fact] void should_reuse_the_same_complete_snapshot() => Request.References(Documents).Equals(_first).ShouldBeTrue();
    [Fact] void should_reuse_a_new_array_of_the_same_immutable_documents() => Request.References([.. Documents]).Equals(_first).ShouldBeTrue();
    [Fact] void should_not_share_manifests_with_another_request() => new McpAttachmentContents.Request().References(Documents).Equals(_first).ShouldBeFalse();

    [Fact]
    void should_rediscover_after_source_changes()
    {
        var changed = Request.References([Document("application.play", Source("Changed.cs"))]);
        changed.Equals(_first).ShouldBeFalse();
        changed.Single().Path.ShouldEqual("Changed.cs");
    }

    [Fact]
    void should_rediscover_after_a_document_moves()
    {
        var moved = WorkspaceDocument.Create(Documents[0].Id, Documents[0].StableKey, PortablePlayPath.Parse("moved.play"), Documents[0].Bytes.AsSpan());
        var changed = Request.References([moved]);
        changed.Equals(_first).ShouldBeFalse();
        changed.Single().Location.Path.ShouldEqual("moved.play");
    }

    [Fact]
    void should_include_the_complete_ordered_document_set_in_the_key()
    {
        var other = Document("other.play", "concept Name : String");
        var expanded = Request.References([.. Documents, other]);
        expanded.Equals(_first).ShouldBeFalse();
        Request.References([other, .. Documents]).Equals(expanded).ShouldBeFalse();
    }

    [Fact]
    void should_bound_the_number_of_retained_snapshots()
    {
        for (var index = 0; index < 4; index++) Request.References([Document($"{index}.play", Source("Handler.cs"))]);
        Request.References(Documents).Equals(_first).ShouldBeFalse();
    }
}
