// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_comparing_agent_exposure;

public class when_changing_metadata : given.agent_models
{
    [Theory]
    [InlineData("write", "read", AgentExposureScopeChange.Widened)]
    [InlineData("read", "write", AgentExposureScopeChange.Narrowed)]
    void should_report_scope_direction_without_new_membership(string before, string after, AgentExposureScopeChange direction)
    {
        var result = Compare(Source + Agents(Query.Replace("scope read", "scope " + before, StringComparison.Ordinal)), Source + Agents(Query.Replace("scope read", "scope " + after, StringComparison.Ordinal)));
        var change = result.Changes.Single();
        change.Change.ShouldEqual(AgentExposureChangeKind.ScopeChanged);
        change.ScopeChange.ShouldEqual(direction);
        result.HasExposureChange.ShouldEqual(true);
    }

    [Fact]
    void should_report_description_only_as_metadata()
    {
        var change = Compare(Source + Agents(Query), Source + Agents(Query.Replace("List projects", "Find projects", StringComparison.Ordinal))).Changes.Single();
        change.Change.ShouldEqual(AgentExposureChangeKind.DescriptionChanged);
        change.Before!.Description.ShouldEqual("List projects");
        change.After!.Description.ShouldEqual("Find projects");
        change.ScopeChange.ShouldEqual(AgentExposureScopeChange.None);
    }
}
