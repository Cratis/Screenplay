// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_reads_and_reducers_use_later_slices : Specification
{
    [Fact]
    void should_report_a_reducer_event_once_at_its_first_rule()
    {
        var result = Parse("module M\n  feature F\n    slice StateView View\n      reducer R => Items\n        on E\n        on E\n    slice StateChange Write\n      event E");
        result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual(["PLAY0516@5"]);
        result.Diagnostics.Single().Message.ShouldContain("uses event 'E' produced by slice 'Write'");
    }

    [Theory]
    [InlineData("command C\n        reads Items as first\n        reads Items as second")]
    [InlineData("reaction C\n        every 15 minutes\n          reads Items as first\n          reads Items as second")]
    void should_name_the_read_model_and_prefer_its_builder(string reader)
    {
        var result = Parse("module M\n  feature F\n    slice StateChange Shape\n      readmodel Items\n    slice Automation Read\n      " + reader + "\n    slice StateView Build\n      event E\n      projection P => Items\n        from E");
        var finding = result.Diagnostics.Single();
        finding.Code.ShouldEqual(DiagnosticCodes.EventFromLaterSlice);
        finding.Severity.ShouldEqual(DiagnosticSeverity.Information);
        finding.Message.ShouldEqual("Slice 'Read' reads read model 'Items' built by slice 'Build' drawn after it. Consider drawing the producer before the consumer.");
    }

    [Theory]
    [InlineData("projection P => Items\n        from E")]
    [InlineData("reducer P => Items\n        on E")]
    void should_exclude_feedback_reads_before_grouping(string builder)
    {
        var result = Parse("module M\n  feature F\n    slice StateChange Read\n      event E\n      command C\n        reads Items\n    slice StateView Build\n      readmodel Items\n      " + builder);
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    void should_group_a_non_feedback_reads_cycle()
    {
        var result = Parse("module M\n  feature F\n    slice StateChange A\n      readmodel Left\n      command C\n        reads Right\n    slice StateChange B\n      readmodel Right\n      command D\n        reads Left");
        result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual(["PLAY0517@6"]);
        result.Diagnostics.Single().Message.ShouldEqual("Timeline group slice 'A', slice 'B' depend on each other's events or read models; reordering these members cannot make every dependency flow left to right.");
    }

    [Fact]
    void should_keep_events_and_read_models_with_the_same_name_distinct()
    {
        var result = Parse("module M\n  feature F\n    slice Automation Read\n      command C\n        reads Items\n      reducer R => Other\n        on Items\n    slice StateView Build\n      readmodel Items\n    slice StateChange Write\n      event Items");
        result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual(["PLAY0516@5", "PLAY0516@7"]);
    }

    [Theory]
    [InlineData("readmodel Items")]
    [InlineData("reducer P => Items\n        on E")]
    [InlineData("projection P\n        variant Items\n          enters on E\n          from E")]
    void should_resolve_declarations_reducers_and_variants(string builder)
    {
        var result = Parse("module M\n  feature F\n    slice StateChange Read\n      command C\n        reads Items\n    slice StateView Build\n      event E\n      " + builder);
        result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual(["PLAY0516@5"]);
    }

    [Fact]
    void should_keep_the_backward_event_edge_when_a_feedback_builder_is_drawn_first()
    {
        var result = Parse("module M\n  feature F\n    slice StateView Build\n      reducer R => Items\n        on E\n    slice StateChange Read\n      event E\n      command C\n        reads Items");
        result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual(["PLAY0516@5"]);
    }

    [Fact]
    void should_keep_the_own_sub_feature_note_for_reads()
    {
        var result = Parse("module M\n  feature F\n    slice StateChange Read\n      command C\n        reads Items\n    feature Child\n      slice StateView Build\n        readmodel Items");
        result.Diagnostics.Single().Message.ShouldContain("own sub-feature");
    }

    [Fact]
    void should_use_the_earliest_duplicate_slice_owner_when_excluding_feedback()
    {
        var application = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateChange Read\n      event E\n      command C\n        reads Items\n    slice StateView Build\n      readmodel Items\n    slice StateView Build\n      projection Other\n        from E").Value!;
        TimelineOrder.In(application).Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual(["PLAY0517@6"]);
    }

    static CompilationResult<Syntax.ApplicationSyntax> Parse(string source) => new ScreenplayCompiler().Compile(source);
}
