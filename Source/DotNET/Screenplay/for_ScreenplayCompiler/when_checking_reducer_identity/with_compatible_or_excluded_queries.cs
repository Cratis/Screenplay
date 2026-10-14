// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_compatible_or_excluded_queries : given.a_reducer_identity_model
{
    [Fact] void should_allow_differently_named_keys_of_the_same_domain() => Warnings(Model.Replace("Month key", "ResourceId key", StringComparison.Ordinal).Replace("by month Month", "by month ResourceId", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_allow_an_inferred_key() => Warnings(Model.Replace("Month key", "ResourceId", StringComparison.Ordinal).Replace("by month Month", "by month ResourceId", StringComparison.Ordinal)).ShouldBeEmpty();
    [Theory]
    [InlineData("Totals optional")]
    [InlineData("observable Totals")]
    void should_check_optional_and_live_instances(string result) => Warnings(Model.Replace("ByMonth => Totals", $"ByMonth => {result}", StringComparison.Ordinal)).Length.ShouldEqual(1);
    [Fact] void should_allow_multiple_producers_in_the_same_nominal_domain() => Warnings((Model + SecondCommand.Replace("SelectionId", "ResourceId", StringComparison.Ordinal)).Replace("Month key", "ResourceId key", StringComparison.Ordinal).Replace("by month Month", "by month ResourceId", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_allow_a_matching_producer_for_an_event_with_mixed_domains_if_other_evidence_is_unknown() => Warnings((Model + SecondCommand).Replace("on Recorded", "on Recorded\n        on Unknown", StringComparison.Ordinal).Replace("event Recorded", "event Unknown\n      event Recorded", StringComparison.Ordinal).Replace("Month key", "ResourceId key", StringComparison.Ordinal).Replace("by month Month", "by month ResourceId", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_allow_several_events_in_one_identity_domain() => Warnings((Model.Replace("on Recorded", "on Recorded\n        on Another", StringComparison.Ordinal)
        .Replace("event Recorded", "event Another\n        resource ResourceId\n      event Recorded", StringComparison.Ordinal)
        + SecondCommand.Replace("SelectionId", "ResourceId", StringComparison.Ordinal).Replace("Select", "Again", StringComparison.Ordinal).Replace("produces Recorded", "produces Another", StringComparison.Ordinal))
        .Replace("Month key", "ResourceId key", StringComparison.Ordinal).Replace("by month Month", "by month ResourceId", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_collection_lookups() => Warnings(Model.Replace("ByMonth => Totals", "ByMonth => Totals[]", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_a_performer() => Warnings(Model.Replace("by month Month", "by month Month\n        performer\n          file Query.cs", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_a_projection_backed_view() => Warnings(Model.Replace("reducer Fold => Totals\n        on Recorded", "projection Fold => Totals\n        from Recorded\n          key resource", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_an_unbuilt_view() => Warnings(Model.Replace("      reducer Fold => Totals\n        on Recorded\n", string.Empty, StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_unknown_argument_types() => Warnings(Model.Replace("by month Month", "by month Missing", StringComparison.Ordinal)).ShouldBeEmpty();
    [Theory]
    [InlineData("Month optional")]
    [InlineData("Month[]")]
    void should_skip_non_scalar_arguments(string type) => Warnings(Model.Replace("by month Month", $"by month {type}", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_composite_lookup_shapes() => Warnings(Model.Replace("by month Month", "by\n          month Month\n          other Month", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_a_public_event_target() => Warnings(Model.Replace("readmodel Totals\n        month Month key", "public event Totals\n        month Month", StringComparison.Ordinal)).ShouldBeEmpty();
}
