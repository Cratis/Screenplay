// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_a_property_with_shared_variant_automap : given.a_shared_variant_property_connection
{
    void Because() => Rename(false);

    [Fact] void should_apply_successfully() => Applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_one_physical_mapping_after_reopen() => PhysicalMappings.ShouldEqual(1);
    [Fact] void should_retain_the_property_report() => Review.GetProperty("property").GetRawText().ShouldEqual(Proposal.GetProperty("property").GetRawText());
    [Fact] void should_retain_the_property_report_in_semantic_diff() => Diff.GetProperty("property").GetRawText().ShouldEqual(Proposal.GetProperty("property").GetRawText());
    [Fact] void should_apply_only_the_reviewed_bytes() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(Reviewed);
    [Fact] void should_preserve_proposed_property_identities_after_apply_and_reopen() => Properties(Reopened).ShouldEqual(Properties(After));
    [Fact] void should_preserve_both_unselected_variant_contracts() => VariantProperties(Reopened).ShouldEqual(VariantProperties(Before));
}
