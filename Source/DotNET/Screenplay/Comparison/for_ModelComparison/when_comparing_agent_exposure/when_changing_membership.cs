// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_comparing_agent_exposure;

public class when_changing_membership : given.agent_models
{
    AgentExposureDifference _exposure;

    void Because() => _exposure = Compare(Source, Source + Agents(Query, Command));

    [Fact] void should_report_each_newly_exposed_operation() => _exposure.Changes.Select(change => change.Change).ShouldContainOnly(AgentExposureChangeKind.Added, AgentExposureChangeKind.Added);
    [Fact] void should_sort_by_qualified_name() => _exposure.Changes.Select(change => change.After!.QualifiedName).ShouldEqual(["Projects.Registration.List.All", "Projects.Registration.Register.Register"]);
    [Fact] void should_be_complete() => _exposure.Complete.ShouldBeTrue();
    [Fact] void should_report_change() => _exposure.HasExposureChange.ShouldEqual(true);
    [Fact] void should_not_claim_ids_in_address_mode() => _exposure.Changes.All(change => change.After!.Id is null).ShouldBeTrue();

    [Fact]
    void should_report_each_removal_in_the_other_direction()
    {
        var removed = Compare(Source + Agents(Query, Command), Source);
        removed.Changes.Select(change => change.Change).ShouldContainOnly(AgentExposureChangeKind.Removed, AgentExposureChangeKind.Removed);
    }

    [Fact]
    void should_report_readdition()
    {
        Compare(Source + Agents(Query), Source);
        Compare(Source, Source + Agents(Query)).Changes.Single().Change.ShouldEqual(AgentExposureChangeKind.Added);
    }

    [Fact]
    void should_leave_earlier_results_unchanged()
    {
        Compare(Source + Agents(Query, Command), Source);
        _exposure.Changes.Count.ShouldEqual(2);
        _exposure.Changes.All(change => change.Change == AgentExposureChangeKind.Added).ShouldBeTrue();
    }
}
