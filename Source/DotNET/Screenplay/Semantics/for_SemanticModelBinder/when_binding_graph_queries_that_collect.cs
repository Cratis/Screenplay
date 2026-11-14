// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_graph_queries_that_collect : given.a_semantic_binder
{
    const string Model =
        "command reads protected\n" +
        "concept PersonId : Uuid\nconcept GroupId : Uuid\n" +
        "type Membership\n  id GroupId\n  since DateTime optional\n" +
        "type Parent\n  id GroupId\n" +
        "module M\n  feature F\n    slice StateView S\n" +
        "      graph G\n        node Person\n          personId PersonId key\n        node Group\n          groupId GroupId key\n" +
        "        edge MEMBER_OF source Person target Group\n          since DateTime optional\n" +
        "        edge PART_OF source Group target Group\n" +
        "      readmodel PersonView\n        id PersonId key\n        groups Membership[]\n" +
        "      readmodel GroupView\n        id GroupId key\n        parent Parent optional\n" +
        "      query People => PersonView[] graph G\n        match person:Person\n        return\n          id = person.personId\n" +
        "          groups = collect person -link:MEMBER_OF-> group:Group\n            id = group.groupId\n            since = link.since\n" +
        "      query Groups => GroupView[] graph G\n        match group:Group\n        return\n          id = group.groupId\n" +
        "          parent = collect group -PART_OF-> parent:Group\n            id = parent.groupId\n";

    const string Flat =
        "command reads protected\n" +
        "concept PersonId : Uuid\n" +
        "module M\n  feature F\n    slice StateView S\n" +
        "      graph G\n        node Person\n          personId PersonId key\n" +
        "      readmodel PersonRow\n        id PersonId key\n" +
        "      query People => PersonRow[] graph G\n        match person:Person\n        return\n          id = person.personId\n";

    ExecutableSemanticModel _model;
    ExecutableSemanticModel _flat;

    void Because()
    {
        var result = Bind(Model);
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        _model = result.Value!.Model;
        _flat = Bind(Flat).Value!.Model;
    }

    SemanticGraphCollect Collect(string query) => _model.Application.GraphQueries.Single(candidate => candidate.Name == query).Collects.Single();

    [Fact] void should_select_esm_v54() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V54);
    [Fact] void should_keep_a_query_that_does_not_collect_on_esm_v40() => _flat.SemanticVersion.ShouldEqual(SemanticVersion.V40);
    [Fact] void should_bind_a_list_as_many() => Collect("People").Cardinality.ShouldEqual(SemanticGraphCollectCardinality.Many);
    [Fact] void should_bind_an_optional_row_as_one() => Collect("Groups").Cardinality.ShouldEqual(SemanticGraphCollectCardinality.One);
    [Fact] void should_bind_the_followed_edge() => (Collect("People").Hop.Type, Collect("People").Hop.Outgoing, Collect("People").Hop.Node.Label).ShouldEqual(("MEMBER_OF", true, "Group"));
    [Fact] void should_start_at_the_root() => Collect("People").From.ShouldEqual("person");
    [Fact] void should_bind_the_row_type() => _model.Application.Types.Single(type => type.Id == Collect("People").Row).Name.ShouldEqual("Membership");
    [Fact] void should_bind_node_and_edge_members() => Collect("People").Members.Select(member => (member.Kind, member.Source)).ShouldEqual([(SemanticGraphCollectMemberKind.Property, "groupId"), (SemanticGraphCollectMemberKind.EdgeProperty, "since")]);
    [Fact] void should_bind_members_to_the_row_properties() => Collect("People").Members.Select(member => member.Property).ShouldEqual(_model.Application.Types.Single(type => type.Name == "Membership").Properties.Select(property => property.Id));
    [Fact] void should_refuse_serializing_below_v54() => Catch.Exception(() => Serialization.SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V53, SemanticVersion.V53, _model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
}
