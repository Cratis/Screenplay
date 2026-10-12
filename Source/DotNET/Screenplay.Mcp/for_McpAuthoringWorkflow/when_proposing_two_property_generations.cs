// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_two_property_generations : given.a_property_generation_connection
{
    void Because() => Evolve(2, true);

    [Fact] void should_match_the_result_json_snapshot() => given.property_result_snapshots.Verify("two-events", Proposal.GetProperty("property"));
    [Fact] void should_keep_unequal_original_revisions_visible() => Proposal.GetProperty("property").GetProperty("generations").EnumerateArray().Select(entry => (entry.GetProperty("from").GetUInt32(), entry.GetProperty("to").GetUInt32())).ShouldEqual(new[] { (2u, 3u), (1u, 2u) });
    [Fact] void should_retain_the_report_in_semantic_diff() => Diff.GetProperty("property").GetRawText().ShouldEqual(Proposal.GetProperty("property").GetRawText());
    [Fact] void should_apply_every_selected_event_atomically() => Applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_every_candidate_identity_after_reopen() => Reopened.IdentityCatalog.Revision.ShouldEqual(After.IdentityCatalog.Revision);
    [Fact] void should_keep_the_audited_predecessor_identity() => Property(Reopened, "Audited", "name", 2).SemanticId.ShouldEqual(Property(Before, "Audited", "name", 2).SemanticId);
    [Fact] void should_keep_the_original_event_contract_ids() => Reopened.IdentityCatalog.EventContracts.Select(entry => entry.Id).ShouldEqual(Before.IdentityCatalog.EventContracts.Select(entry => entry.Id));
    [Fact] void should_install_the_exact_reviewed_source() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(Reviewed);
}
