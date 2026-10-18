// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_comparing_the_documented_synthetic_snapshot : Specification
{
    ObservationReport _report = null!;

    void Because()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
        var workspace = ComparedModel.FromSources("Projects", new Dictionary<string, string> { ["application.play"] = source }).Workspace;
        Assert.True(workspace.Compilation.Success);
        var snapshot = ObservationSnapshot.Create(
            "Projects",
            "tenant",
            new("log", true, null, null, null, []),
            [new("Registered", 1)],
            [],
            [],
            [],
            [],
            DateTimeOffset.UnixEpoch);
        _report = ObservationComparison.Compare(workspace.Compilation.Value!, snapshot);
    }

    [Fact] void should_establish_no_drift() => _report.HasDrift.ShouldEqual(false);
    [Fact] void should_be_complete() => _report.Complete.ShouldBeTrue();
    [Fact] void should_report_event_absence_as_information() => _report.Findings.Single().Kind.ShouldEqual(ObservationFindingKind.EventNotAppended);
}
