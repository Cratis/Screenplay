// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpIndex;

public class when_preserving_registered_fence_ownership
{
    [Theory]
    [InlineData("")]
    [InlineData("numbers exact\n")]
    void should_share_the_snapshot_registry_with_compilation_and_physical_index_discovery(string prefix)
    {
        var root = prefix + "policy P\n  ```custom\nimport \"fake.play\"\nnumbers legacy\neventsource Foreign\n  stream Hidden\n  ```\nimport \"source.play\"\n";
        var child = prefix + "eventsource Orders\n  stream Changes\n";
        var snapshot = new McpSnapshot([given.synthetic_model.Document("root", root), given.synthetic_model.Document("source", child)], new ScreenplayLanguageRegistry(["custom"]));
        snapshot.Compilation.Success.ShouldBeTrue();
        snapshot.Compilation.Value!.SourceOptions.ShouldEqual(prefix.Length == 0 ? SourceOptions.Legacy : SourceOptions.Exact);
        snapshot.Index.Find("Foreign", "EventSource").ShouldBeEmpty();
        snapshot.Index.Find("Orders", "EventSource").Length.ShouldEqual(1);
        snapshot.ParsedDocumentCount.ShouldEqual(2);
    }
}
