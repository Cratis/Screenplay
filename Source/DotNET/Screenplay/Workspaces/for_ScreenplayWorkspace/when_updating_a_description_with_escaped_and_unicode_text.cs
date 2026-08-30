// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_a_description_with_escaped_and_unicode_text : given.a_workspace_with_an_escaped_slice_description
{
    const string NewDescription = "New \"desc\"\twith tab\nand emoji \U0001F600 and back\\slash and 日本語";
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new UpdateSliceDescription
    {
        SemanticId = SliceId,
        ExpectedCurrentDescription = OriginalDescription,
        NewDescription = NewDescription
    }));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_round_trip_the_new_description_through_the_updated_source_map() => DecodedNewDescription().ShouldEqual(NewDescription);
    [Fact] void should_keep_the_updated_bytes_strict_utf8() => _result.Workspace!.Documents.Single(document => document.Id == Registration.Id).Text.Contains('\uFFFD').ShouldBeFalse();

    string DecodedNewDescription()
    {
        var workspace = _result.Workspace!;
        var document = workspace.Documents.Single(candidate => candidate.Id == Registration.Id);
        var entry = workspace.Compilation.Value!.SourceMap.Entries.Single(candidate => candidate.SemanticId == SliceId && candidate.Role == SemanticSourceMapRole.Description);
        return StringLiteral.Unescape(document.Text.Substring(entry.Span.Start, entry.Span.Length));
    }
}
