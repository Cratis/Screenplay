// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_comparing_agent_exposure;

public class when_compilation_is_unavailable : given.agent_models
{
    [Theory]
    [InlineData("query Missing\n    scope read\n    description \"Missing\"")]
    [InlineData("command All\n    scope write\n    description \"Wrong kind\"")]
    [InlineData("command Register\n    scope read\n    description \"Wrong scope\"")]
    [InlineData("query All\n    scope read")]
    void should_report_unknown_not_empty_for_invalid_agent_entries(string entry) => AssertUnknown(Source + Agents(entry));

    [Fact] void should_reject_ambiguous_short_references() => AssertUnknown(Source.Replace("query History", "query All", StringComparison.Ordinal) + Agents(Query.Replace("Projects.Registration.List.All", "All", StringComparison.Ordinal)));
    [Fact] void should_reject_duplicate_exposure() => AssertUnknown(Source + Agents(Query, Query));
    [Fact] void should_reject_unrelated_binding_errors() => AssertUnknown(Source.Replace("=> Projects optional", "=> Missing optional", StringComparison.Ordinal) + Agents(Query));
    [Fact] void should_reject_syntax_errors() => AssertUnknown("unknown root\n" + Source + Agents(Query));

    void AssertUnknown(string invalid)
    {
        var after = Compare(Source, invalid);
        after.Complete.ShouldBeFalse();
        after.HasExposureChange.ShouldBeNull();
        after.AfterAvailability.ShouldEqual(AgentExposureAvailability.CompilationFailed);
        after.BeforeAvailability.ShouldEqual(AgentExposureAvailability.Available);
        after.Changes.ShouldBeEmpty();
        var before = Compare(invalid, Source);
        before.HasExposureChange.ShouldBeNull();
        before.BeforeAvailability.ShouldEqual(AgentExposureAvailability.CompilationFailed);
        before.Changes.ShouldBeEmpty();
    }
}
