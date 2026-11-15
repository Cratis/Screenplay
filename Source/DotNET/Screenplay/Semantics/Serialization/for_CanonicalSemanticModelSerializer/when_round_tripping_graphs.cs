// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.for_Documentation.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_graphs : for_SemanticModelBinder.given.a_semantic_binder
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;
    ExecutableSemanticModel _read;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        _model = Bind(File.ReadAllText(Path.Combine(root, "Samples/Organization/organization.play"))).Value!.Model;
        _bytes = SemanticModelSerializer.Serialize(_model);
        _text = Encoding.UTF8.GetString(_bytes);
        _read = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_select_v54_for_its_collecting_queries() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V54);
    [Fact] void should_round_trip_to_the_same_bytes() => SemanticModelSerializer.Serialize(_read).ShouldEqual(_bytes);
    [Fact] void should_keep_the_graph() => _read.Application.Graphs.Single().Edges.Select(edge => edge.Type).ShouldEqual("HAS_ROLE", "MEMBER_OF", "PART_OF");
    [Fact] void should_keep_the_edge_identity() => _read.Application.Graphs.Single().Edges.Single(edge => edge.Type == "HAS_ROLE").IdentifiedBy.ShouldEqual("assignmentId");
    [Fact] void should_keep_the_projection_handlers() => _read.Application.GraphProjections.Single().Nodes.Single(node => node.Name == "Person").Handlers.Count(handler => handler.Removes && handler.Detach).ShouldEqual(1);
    [Fact] void should_keep_the_graph_specifications() => _read.Application.GraphSpecifications.Length.ShouldEqual(12);
    [Fact] void should_write_the_graph() => _text.ShouldContain("\"graphs\":[{\"name\":\"Organization\"");
    [Fact] void should_write_the_projection() => _text.ShouldContain("\"graphProjections\":[{\"name\":\"OrganizationChart\",\"graph\":\"Organization\"");
    [Fact] void should_refuse_graphs_below_v38() => Catch.Exception(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V37, SemanticVersion.V37, _model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_keep_the_collecting_queries() => _read.Application.GraphQueries.Count(query => !query.Collects.IsDefaultOrEmpty).ShouldEqual(4);
    [Fact] void should_refuse_collecting_queries_below_v54() => Catch.Exception(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V53, SemanticVersion.V53, _model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV37Support.Supports(LanguageVersion.V38, SemanticVersion.V38).ShouldBeFalse();
    [Fact] void should_read_the_model_through_the_unknown_preserving_reader() => UnknownPreservingSemanticModelReader.Read(_bytes, new EsmAdmissionRange(54, 0, 0)).Model.Application.Graphs.Length.ShouldEqual(1);
}
