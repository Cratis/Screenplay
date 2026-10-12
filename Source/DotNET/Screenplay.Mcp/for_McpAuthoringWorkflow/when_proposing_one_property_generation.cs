// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_one_property_generation : given.a_property_generation_connection
{
    void Because() => Evolve(1, true);

    [Fact] void should_match_the_result_json_snapshot() => given.property_result_snapshots.Verify("one-event", Proposal.GetProperty("property"));
    [Fact] void should_retain_the_same_json_for_review() => Review.GetProperty("property").GetRawText().ShouldEqual(Proposal.GetProperty("property").GetRawText());
    [Fact] void should_preserve_the_historical_property_identity() => Property(Reopened, "Registered", "name", 1).SemanticId.ShouldEqual(Property(Before, "Registered", "name", 1).SemanticId);
    [Fact] void should_assign_a_distinct_new_property_identity() => Property(Reopened, "Registered", "label", 2).SemanticId.Equals(Property(Before, "Registered", "name", 1).SemanticId).ShouldBeFalse();
    [Fact] void should_keep_unselected_event_fields_and_identities() => Property(Reopened, "Audited", "name", 2).SemanticId.ShouldEqual(Property(Before, "Audited", "name", 2).SemanticId);
    [Fact] void should_apply_the_reviewed_source() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(Reviewed);
}
