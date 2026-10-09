// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_translating_through_events : given.a_compiler
{
    const string Contracts = "import Outside.Arrived from \"other/store\"\nmodule Sales\n  feature Orders\n    slice StateChange Facts\n      event Changed\n      public event Published\n      public event Second\n";
    const string Outbound = "    slice Translate Publish\n      direction outbound\n      projection Publisher => Published\n        from Changed\n          id = $eventSourceId\n";

    [Theory]
    [InlineData("StateView", "projection Publisher => Published\n        from Changed\n          id = $eventSourceId", DiagnosticCodes.EventTargetOutsideOutboundTranslation)]
    [InlineData("StateView", "reducer Publisher => Changed\n        on Changed\n          file Reduce.cs", DiagnosticCodes.EventTargetOutsideOutboundTranslation)]
    [InlineData("Translate", "direction inbound\n      projection Publisher => Published\n        from Changed\n          id = $eventSourceId", DiagnosticCodes.EventTargetOutsideOutboundTranslation)]
    [InlineData("Translate", "direction outbound\n      projection Publisher => Changed\n        from Changed\n          id = $eventSourceId", DiagnosticCodes.OutboundTranslationOutput)]
    [InlineData("Translate", "direction outbound\n      projection Publisher => Arrived\n        from Changed\n          id = $eventSourceId", DiagnosticCodes.ForeignPublicEventProduced)]
    [InlineData("Translate", "direction outbound\n      projection Publisher => Published\n        from Published\n          id = $eventSourceId", DiagnosticCodes.OutboundTranslationInput)]
    [InlineData("Translate", "direction outbound\n      projection Publisher => Published\n        from Arrived\n          id = $eventSourceId", DiagnosticCodes.OutboundTranslationInput)]
    [InlineData("Translate", "direction outbound\n      projection First => Published\n        from Changed\n          id = $eventSourceId\n      projection Other => Second\n        from Changed\n          id = $eventSourceId", DiagnosticCodes.OutboundPublicEventCount)]
    [InlineData("Translate", "capture Feed\n        source events\n          from Arrived\n        key id", DiagnosticCodes.EventsSourceOutsideInboundTranslation)]
    [InlineData("Automation", "capture Feed\n        source events\n          from Arrived\n        key id", DiagnosticCodes.EventsSourceOutsideInboundTranslation)]
    [InlineData("Translate", "direction inbound\n      capture Feed\n        source events\n          from Changed\n        key id\n        append Changed", DiagnosticCodes.InboundTranslationInput)]
    [InlineData("Translate", "direction inbound\n      capture Feed\n        source events\n          from Arrived\n        key id\n        append Published", DiagnosticCodes.InboundTranslationOutput)]
    [InlineData("Translate", "direction inbound\n      capture Feed\n        source events\n        key id", DiagnosticCodes.InvalidCaptureEventsSource)]
    [InlineData("Translate", "direction inbound\n      capture Feed\n        source events\n          route /x\n        key id", DiagnosticCodes.InvalidCaptureEventsSource)]
    [InlineData("Translate", "direction inbound\n      capture Feed\n        source events\n          from Arrived\n          from Arrived\n        key id", DiagnosticCodes.InvalidCaptureEventsSource)]
    void should_report_each_violation(string kind, string body, string code)
    {
        var result = _compiler.Compile(Contracts + $"    slice {kind} Transfer\n      {body}\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == code && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Fact]
    void should_accept_an_event_target_projection_in_an_outbound_translation()
    {
        var result = _compiler.Compile(Contracts + Outbound);
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    void should_accept_an_event_target_reducer_in_an_outbound_translation()
    {
        var result = _compiler.Compile(Contracts + "    slice Translate Publish\n      direction outbound\n      reducer Publisher => Published\n        on Changed\n          file Reduce.cs\n");
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    void should_accept_events_source_in_an_inbound_translation()
    {
        var result = _compiler.Compile(Contracts + "    slice Translate Track\n      direction inbound\n      event Received\n      capture Feed\n        source events\n          from Arrived\n        key id\n        append Received\n");
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var source = result.Value!.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Captures).Single().Source!;
        CaptureEventsSource.Events(source).Select(setting => setting.Value).ShouldEqual(["Arrived"]);
    }

    [Fact]
    void should_keep_a_read_model_named_like_its_target_a_read_model()
    {
        var result = _compiler.Compile(Contracts + "    slice StateView Orders\n      readmodel Changed\n      projection Changed => Changed\n        from Changed\n          id = $eventSourceId\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EventTargetOutsideOutboundTranslation).ShouldBeFalse();
    }

    [Fact]
    void should_admit_an_events_source_over_a_locally_declared_foreign_event()
    {
        const string Source = "module Sales\n  feature Orders\n    slice Translate Track\n      direction inbound\n      event Arrived from \"other/store\"\n        id String\n      event Received\n        id String\n      capture Feed\n        source events\n          from Arrived\n        key id\n        append Received\n          id = $.id\n";
        var bound = Bind(_compiler.Compile(Source).Value!, Source);
        Assert.True(bound.Success, string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V9);
    }

    [Fact]
    void should_refuse_an_events_source_over_an_event_that_has_no_local_shape()
    {
        var result = _compiler.Compile(Contracts + "    slice Translate Track\n      direction inbound\n      event Received\n      capture Feed\n        source events\n          from Arrived\n        key id\n        append Received\n");
        var bound = Bind(result.Value!);
        bound.Success.ShouldBeFalse();
        bound.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("no local shape")).ShouldBeTrue();
    }

    [Fact]
    void should_admit_an_event_target_projection_in_an_outbound_translation()
    {
        const string Source = "module Sales\n  feature Orders\n    slice StateChange Facts\n      event Changed\n        id String\n    slice Translate Publish\n      direction outbound\n      public event Published\n        id String\n      projection Publisher => Published\n        from Changed\n";
        var bound = Bind(_compiler.Compile(Source).Value!, Source);
        Assert.True(bound.Success, string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V9);
    }

    static CompilationResult<SemanticCompilation> Bind(ApplicationSyntax application, string source = Contracts)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Sales"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        return new SemanticModelBinder().Bind("Sales", application, SemanticDocumentSet.Create([document], catalog));
    }
}
