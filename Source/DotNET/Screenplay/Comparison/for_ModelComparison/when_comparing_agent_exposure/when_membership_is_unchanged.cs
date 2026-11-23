// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_comparing_agent_exposure;

public class when_membership_is_unchanged : given.agent_models
{
    [Theory]
    [InlineData("", "")]
    [InlineData("\nagents\n  tokens personal", "\nagents\n  tokens personal")]
    [InlineData("", "\nagents\n  tokens personal")]
    [InlineData("\nagents\n  tokens personal", "")]
    void should_treat_successful_absence_and_empty_blocks_as_known_empty(string before, string after)
    {
        var result = Compare(Source + before, Source + after);
        result.Complete.ShouldBeTrue();
        result.HasExposureChange.ShouldEqual(false);
        result.Changes.ShouldBeEmpty();
    }

    [Fact]
    void should_not_expose_a_new_declaration() => Compare(Source, Source.Replace("query All", "query Extra => Projects optional\n        by projectId Uuid\n      query All", StringComparison.Ordinal)).Changes.ShouldBeEmpty();

    [Fact]
    void should_compare_resolved_references_not_authored_aliases() => Compare(Source + Agents(Query), Source + Agents(Query.Replace("Projects.Registration.List.All", "All", StringComparison.Ordinal))).HasExposureChange.ShouldEqual(false);

    [Fact]
    void should_ignore_entry_order() => Compare(Source + Agents(Query, Command), Source + Agents(Command, Query)).HasExposureChange.ShouldEqual(false);

    [Fact]
    void should_not_classify_matching_property_names_as_exposure() => Compare(Source, Source.Replace("name String", "All String", StringComparison.Ordinal)).Changes.ShouldBeEmpty();
}
