// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_analyzing_findings : Specification
{
    [Fact]
    void should_return_structured_edges_without_changing_diagnostics()
    {
        var application = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateView Consumer\n      projection P\n        from E\n    slice StateChange Producer\n      event E\n").Value!;
        var findings = TimelineOrder.Analyze(application);
        var finding = findings.Single();
        finding.Diagnostic.Code.ShouldEqual(DiagnosticCodes.EventFromLaterSlice);
        finding.ConsumerScope.ShouldEqual(["M", "F", "Consumer"]);
        finding.ProducerScope.ShouldEqual(["M", "F", "Producer"]);
        finding.Event.ShouldEqual("E");
        finding.Left.ShouldEqual("slice:Consumer");
        finding.Right.ShouldEqual("slice:Producer");
        finding.OwnSubFeature.ShouldBeFalse();
        findings.Select(value => value.Diagnostic).ShouldEqual(TimelineOrder.In(application));
    }

    [Fact]
    void should_identify_own_sub_features_and_cycle_group_members()
    {
        var compiler = new ScreenplayCompiler();
        var own = compiler.Parse("module M\n  feature F\n    slice StateView Consumer\n      projection P\n        from E\n    feature Sub\n      slice StateChange Producer\n        event E\n").Value!;
        TimelineOrder.Analyze(own).Single().OwnSubFeature.ShouldBeTrue();
        var cycle = compiler.Parse("module M\n  feature A\n    slice StateChange First\n      event X\n      projection P\n        from Y\n  feature B\n    slice StateChange Second\n      event Y\n      projection Q\n        from X\n").Value!;
        var group = TimelineOrder.Analyze(cycle).Single();
        group.Diagnostic.Code.ShouldEqual(DiagnosticCodes.TimelineCycleGroup);
        group.Members.Order(StringComparer.Ordinal).ShouldEqual(["container:A", "container:B"]);
    }
}
