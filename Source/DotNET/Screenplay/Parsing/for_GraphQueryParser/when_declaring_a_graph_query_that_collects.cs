// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_GraphQueryParser;

public class when_declaring_a_graph_query_that_collects : Specification
{
    const string Types =
        "type Membership\n  id GroupId\n  since DateTime\n" +
        "type Member\n  id PersonId\n  name PersonName\n";

    const string Declarations =
        GraphDocuments.Graph +
        "readmodel PersonWithGroups\n  id PersonId key\n  name PersonName\n  groups Membership[]\n" +
        "readmodel GroupWithMembers\n  id GroupId key\n  members Member[]\n  first Member optional\n";

    const string Valid =
        "query PeopleInGroup => PersonWithGroups[] graph Organization\n" +
        "  filter groupId GroupId\n" +
        "  match person:Person\n" +
        "  where group.groupId = $groupId\n" +
        "  return\n" +
        "    id = person.personId\n" +
        "    name = person.name\n" +
        "    groups = collect person -link:MEMBER_OF-> group:Group\n" +
        "      id = group.groupId\n" +
        "      since = link.since\n";

    const string Incoming =
        "query GroupsWithMembers => GroupWithMembers[] graph Organization\n" +
        "  match group:Group\n" +
        "  return\n" +
        "    id = group.groupId\n" +
        "    members = collect group <-MEMBER_OF- member:Person\n" +
        "      id = member.personId\n" +
        "      name = member.name\n" +
        "    first = collect group <-MEMBER_OF- leader:Person\n" +
        "      id = leader.personId\n" +
        "      name = leader.name\n";

    GraphQuerySyntax _query;

    static CompilationResult<ApplicationSyntax> Compile(string query, string declarations = Declarations) =>
        new ScreenplayCompiler().Compile(GraphDocuments.Concepts.Replace("type Address", Types + "type Address", StringComparison.Ordinal) + "module M\n  feature F\n    slice StateView S\n" + GraphDocuments.Indent(declarations + query));

    static string[] Codes(string query, string declarations = Declarations) =>
        [.. Compile(query, declarations).Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code)];

    static GraphQuerySyntax Query(string query) => Compile(query).Value!.Modules.Single().Features.Single().Slices.Single().GraphQueries.Single();

    void Because() => _query = Query(Valid);

    [Fact] void should_be_valid() => Codes(Valid).ShouldBeEmpty();
    [Fact] void should_keep_the_scalar_returns_apart() => _query.Returns.Select(@return => @return.Property).ShouldEqual(["id", "name"]);
    [Fact] void should_read_the_collected_property() => _query.Collects.Single().Property.ShouldEqual("groups");
    [Fact] void should_start_at_the_root() => _query.Collects.Single().From.ShouldEqual("person");
    [Fact] void should_read_the_followed_edge() => (_query.Collects.Single().Hop.Type, _query.Collects.Single().Hop.Outgoing, _query.Collects.Single().Hop.Alias).ShouldEqual(("MEMBER_OF", true, "link"));
    [Fact] void should_read_the_reached_node() => (_query.Collects.Single().Hop.Node.Alias, _query.Collects.Single().Hop.Node.Label).ShouldEqual(("group", "Group"));
    [Fact] void should_read_the_row() => _query.Collects.Single().Returns.Select(@return => $"{@return.Property}={@return.Alias}.{@return.Source}").ShouldEqual(["id=group.groupId", "since=link.since"]);
    [Fact] void should_accept_an_incoming_edge_and_a_single_row() => Codes(Incoming).ShouldBeEmpty();
    [Fact] void should_read_an_incoming_edge() => Query(Incoming).Collects.First().Hop.Outgoing.ShouldBeFalse();
    [Fact] void should_report_a_collect_without_a_pattern() => Codes(Valid.Replace("collect person -link:MEMBER_OF-> group:Group", "collect", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.InvalidGraphCollect);
    [Fact] void should_report_a_collect_of_two_edges() => Codes(Valid.Replace("-> group:Group\n", "-> group:Group <-MEMBER_OF- other:Person\n", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.InvalidGraphCollect);
    [Fact] void should_report_a_collect_from_another_alias() => Codes(Valid.Replace("collect person", "collect stranger", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.InvalidGraphCollect);
    [Fact] void should_report_a_row_read_from_the_root() => Codes(Valid.Replace("id = group.groupId", "id = person.personId", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.InvalidGraphCollect);
    [Fact] void should_report_a_malformed_row() => Codes(Valid.Replace("      since = link.since\n", "      since link.since\n", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.InvalidGraphCollect);
    [Fact] void should_report_a_collect_into_a_scalar() => Codes(Valid.Replace("groups = collect", "name = collect", StringComparison.Ordinal).Replace("    name = person.name\n", string.Empty, StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.GraphCollectTargetMismatch);
    [Fact] void should_report_a_collect_into_a_required_single_row() => Codes(Valid, Declarations.Replace("groups Membership[]", "groups Membership", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.GraphCollectTargetMismatch);
    [Fact] void should_report_a_row_property_the_type_lacks() => Codes(Valid + "      extra = group.groupId\n").ShouldContain(DiagnosticCodes.GraphCollectTargetMismatch);
    [Fact] void should_report_a_missing_row_property() => Codes(Valid.Replace("      since = link.since\n", string.Empty, StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.GraphCollectTargetMismatch);
    [Fact] void should_report_a_row_property_of_another_type() => Codes(Valid.Replace("id = group.groupId", "id = link.since", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.GraphCollectTargetMismatch);
    [Fact] void should_report_a_nested_collect() => Codes(Valid + "      more = collect group <-MEMBER_OF- other:Person\n        id = other.personId\n").ShouldContain(DiagnosticCodes.NestedGraphCollect);
    [Fact] void should_report_a_match_that_follows_an_edge() => Codes(Valid.Replace("match person:Person\n", "match person:Person -MEMBER_OF-> other:Group\n", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.GraphCollectQueryShape);
    [Fact] void should_report_a_second_match() => Codes(Valid.Replace("match person:Person\n", "match person:Person\n  optional match person:Person -MEMBER_OF-> other:Group\n", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.GraphCollectQueryShape);
    [Fact] void should_report_a_condition_on_a_collected_edge() => Codes(Valid.Replace("where group.groupId = $groupId", "where link.since = \"2026-01-01T00:00:00Z\"", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.GraphCollectQueryShape);
    [Fact] void should_report_a_collect_against_the_edge_direction() => Codes(Valid.Replace("-link:MEMBER_OF->", "<-link:MEMBER_OF-", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.GraphQueryEdgeMismatch);
    [Fact] void should_report_a_collected_alias_used_twice() => Codes(Valid.Replace("group:Group", "person:Group", StringComparison.Ordinal)).ShouldContain(DiagnosticCodes.DuplicateGraphQueryAlias);
}
