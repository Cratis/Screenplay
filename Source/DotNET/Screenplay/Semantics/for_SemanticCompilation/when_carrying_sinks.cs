// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticCompilation;

public class when_carrying_sinks : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source = """
        module M
          feature F
            slice StateView S
              readmodel Row
                id String key
                value String
              projection Build => Row
                from Recorded
                  id = $eventSourceId
                  value = value
              event Recorded
                value String
        """;

    static readonly string _graph = GraphDocuments.Document(GraphDocuments.Graph);

    [Fact]
    void should_map_a_read_model_to_its_sink_without_changing_executable_bytes_or_revision()
    {
        var plain = Bind(Source);
        var sunk = Bind("sink Reporting for readmodels\n" + Source.Replace("      readmodel Row\n", "      readmodel Row\n        sink Reporting\n", StringComparison.Ordinal));
        plain.Success.ShouldBeTrue();
        sunk.Success.ShouldBeTrue();
        plain.Value!.ReadModelSinks.ShouldBeEmpty();
        plain.Value.Sinks.ShouldBeEmpty();
        var compilation = sunk.Value!;
        var model = compilation.Model.Application.Modules.Single().Features.Single().Slices.Single().ReadModels.Single();
        compilation.ReadModelSinks[model.Id].ShouldEqual("Reporting");
        compilation.Sinks.Single().ShouldEqual(new SemanticSink("Reporting", SemanticSinkKind.ReadModels));
        compilation.GraphSinks.ShouldBeEmpty();
        compilation.Model.Revision.ShouldEqual(plain.Value.Model.Revision);
        SemanticModelSerializer.Serialize(compilation.Model).SequenceEqual(SemanticModelSerializer.Serialize(plain.Value.Model)).ShouldBeTrue();
        sunk.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax && diagnostic.Message.Contains("Reporting", StringComparison.Ordinal)).ShouldEqual(2);
    }

    [Fact]
    void should_map_a_graph_to_its_sink_without_changing_executable_bytes_or_revision()
    {
        var plain = Bind(_graph);
        var sunk = Bind("sink People for graphs\n" + _graph.Replace("graph Organization\n", "graph Organization\n        sink People\n", StringComparison.Ordinal));
        plain.Success.ShouldBeTrue();
        sunk.Success.ShouldBeTrue();
        var compilation = sunk.Value!;
        compilation.GraphSinks["Organization"].ShouldEqual("People");
        compilation.Sinks.Single().ShouldEqual(new SemanticSink("People", SemanticSinkKind.Graphs));
        compilation.ReadModelSinks.ShouldBeEmpty();
        compilation.Model.Revision.ShouldEqual(plain.Value!.Model.Revision);
        SemanticModelSerializer.Serialize(compilation.Model).SequenceEqual(SemanticModelSerializer.Serialize(plain.Value.Model)).ShouldBeTrue();
    }

    [Fact]
    void should_keep_sinks_through_every_sidecar_copy()
    {
        var compilation = Bind("sink Reporting for readmodels\n" + Source.Replace("      readmodel Row\n", "      readmodel Row\n        sink Reporting\n", StringComparison.Ordinal)).Value!;
        var origins = ImmutableDictionary<SemanticId, Syntax.Specifications.EffectiveSpecification>.Empty;
        var copied = compilation.WithEnvironment(EnvironmentRequirements.None).WithSpecificationOrigins(origins).WithPassiveReadModels([]);
        copied.Sinks.SequenceEqual(compilation.Sinks).ShouldBeTrue();
        copied.ReadModelSinks.ShouldEqual(compilation.ReadModelSinks);
        copied.GraphSinks.ShouldEqual(compilation.GraphSinks);
        var recreated = SemanticCompilation.Create(compilation.Model, compilation.Documents, compilation.SourceMap).WithSidecarsOf(compilation);
        recreated.ReadModelSinks.ShouldEqual(compilation.ReadModelSinks);
        recreated.PassiveReadModels.ShouldEqual(compilation.PassiveReadModels);
    }

    [Fact]
    void should_default_to_empty_when_recreated_from_portable_semantics()
    {
        var compilation = Bind("sink Reporting for readmodels\n" + Source.Replace("      readmodel Row\n", "      readmodel Row\n        sink Reporting\n", StringComparison.Ordinal)).Value!;
        var recreated = SemanticCompilation.Create(compilation.Model, compilation.Documents, compilation.SourceMap);
        recreated.Sinks.ShouldBeEmpty();
        recreated.ReadModelSinks.ShouldBeEmpty();
        recreated.GraphSinks.ShouldBeEmpty();
    }
}
