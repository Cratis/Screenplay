// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_paging_a_comparison : given.a_semantic_comparison
{
    JsonElement _second;
    JsonElement _repeated;
    Exception? _stale;
    Exception? _unpinned;

    void Because()
    {
        Propose(Source.Replace("event Registered\n        name String", "event Registered\n        name String\n        extra String", StringComparison.Ordinal));
        Diff = Read(new { limit = 1 });
        _repeated = Read(new { limit = 1 });
        _second = Read(new { offset = 1, limit = 1, expectedSourceRevision = Diff.GetProperty("sourceRevision").GetString() });
        _stale = Catch.Exception(() => Read(new { offset = 1, limit = 1, expectedSourceRevision = "stale" }));
        _unpinned = Catch.Exception(() => Read(new { offset = 1, limit = 1 }));
    }

    [Fact] void should_produce_identical_bytes_for_identical_inputs() => _repeated.GetRawText().ShouldEqual(Diff.GetRawText());
    [Fact] void should_page_in_stable_order() => _second.GetProperty("page").GetProperty("offset").GetInt32().ShouldEqual(1);
    [Fact] void should_bind_pages_to_the_proposal_revision() => _second.GetProperty("page").GetProperty("revision").GetString().ShouldEqual(Proposal.Workspace.Revision.ToString());
    [Fact] void should_refuse_stale_continuation() => _stale!.Message.ShouldContain("StaleRevision");
    [Fact] void should_require_a_revision_on_continuation() => _unpinned!.Message.ShouldContain("expectedSourceRevision");
}
