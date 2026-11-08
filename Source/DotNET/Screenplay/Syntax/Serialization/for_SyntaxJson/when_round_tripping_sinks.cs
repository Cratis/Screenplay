// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_sinks : Specification
{
    readonly ReadModelSyntax _model = new("Row", [], SourceLocation.Start);
    readonly GraphSyntax _graph = new("Organization", [], [], SourceLocation.Start);
    readonly ApplicationSyntax _application = new([], [], [], [], SourceLocation.Start);

    [Fact]
    void should_omit_a_missing_read_model_sink() => SyntaxJson.Serialize(_model).TryGetProperty("sink", out _).ShouldBeFalse();

    [Fact]
    void should_omit_a_missing_graph_sink() => SyntaxJson.Serialize(_graph).TryGetProperty("sink", out _).ShouldBeFalse();

    [Fact]
    void should_omit_no_sinks() => SyntaxJson.Serialize(_application).TryGetProperty("sinks", out _).ShouldBeFalse();

    [Fact]
    void should_default_missing_to_no_sink() => ((ReadModelSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_model))).Sink.ShouldBeNull();

    [Fact]
    void should_preserve_a_read_model_sink()
    {
        var json = SyntaxJson.Serialize(_model with { Sink = "Reporting" });
        json.GetProperty("sink").GetString().ShouldEqual("Reporting");
        ((ReadModelSyntax)SyntaxJson.Deserialize(json)).Sink.ShouldEqual("Reporting");
    }

    [Fact]
    void should_preserve_a_graph_sink() => ((GraphSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_graph with { Sink = "People" }))).Sink.ShouldEqual("People");

    [Fact]
    void should_preserve_declared_sinks()
    {
        var application = _application with { Sinks = [new SinkSyntax("Reporting", SinkKind.ReadModels, SourceLocation.Start), new SinkSyntax("People", SinkKind.Graphs, SourceLocation.Start)] };
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(application));
        restored.Sinks.Select(sink => (sink.Name, sink.Stores)).ShouldContainOnly(("Reporting", SinkKind.ReadModels), ("People", SinkKind.Graphs));
    }
}
