// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_reviewing_exact_proposed_identity_state : given.a_durable_workspace
{
    JsonElement _before;
    JsonElement _after;

    void Because()
    {
        var session = new McpWorkspaces(Root);
        _ = session.Open(McpJson.Empty);
        var arguments = Arguments(new
        {
            expectedRevision = Original.Revision.ToString(),
            expectedCatalogRevision = Original.IdentityCatalog.Revision.ToString(),
            operation = "move-document",
            documentId = Original.Documents.Single().Id.ToString(),
            path = "renamed.play"
        });
        var proposal = Result(session.Propose(arguments, expand: false));
        var proposalId = proposal.GetProperty("proposalId").GetString();
        var change = proposal.GetProperty("stateChange");
        _before = Result(session.State(Arguments(new { proposalId, view = "before", expectedStateRevision = change.GetProperty("beforeRevision").GetString() })));
        _after = Result(session.State(Arguments(new { proposalId, view = "after", expectedStateRevision = change.GetProperty("afterRevision").GetString() })));
    }

    [Fact] void should_expose_the_exact_original_state() => _before.GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64().AsSpan().SequenceEqual(OriginalState).ShouldBeTrue();
    [Fact] void should_expose_the_exact_proposed_state() => _after.GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64().AsSpan().SequenceEqual(McpState.Serialize(Proposal.Workspace)).ShouldBeTrue();
    [Fact] void should_not_install_the_proposed_state() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
}
