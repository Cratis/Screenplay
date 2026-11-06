// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_GraphQueryParser;

public class when_declaring_a_graph_query : Specification
{
    static readonly string Declarations =
        GraphDocuments.Graph.Replace("    since DateTime\n", "    since DateTime optional\n") +
        "readmodel Membership\n  personId PersonId key\n  person PersonName\n  group GroupId\n  since DateTime optional\n" +
        "readmodel Counted\n  groupId GroupId key\n  people Int\n";

    static CompilationResult<ApplicationSyntax> Compile(string query) =>
        new ScreenplayCompiler().Compile(GraphDocuments.Document(Declarations + query));

    static string[] Codes(string query) =>
        [.. Compile(query).Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code)];

    const string Valid =
        "query MembersOf => Membership[] graph Organization\n" +
        "  filter groupId GroupId\n" +
        "  match person:Person -link:MEMBER_OF-> group:Group\n" +
        "  where group.groupId = $groupId\n" +
        "  return\n" +
        "    personId = person.personId\n" +
        "    person = person.name\n" +
        "    group = group.groupId\n" +
        "    since = link.since\n";

    GraphQuerySyntax _query;

    void Because() => _query = Compile(Valid).Value!.Modules.Single().Features.Single().Slices.Single().GraphQueries.Single();

    [Fact] void should_read_the_name_and_graph() => (_query.Name, _query.Graph).ShouldEqual(("MembersOf", "Organization"));
    [Fact] void should_return_a_collection_of_the_read_model() => (_query.ReturnType.Name, _query.ReturnType.IsCollection).ShouldEqual(("Membership", true));
    [Fact] void should_read_the_hop() => (_query.Matches.Single().Hops.Single().Type, _query.Matches.Single().Hops.Single().Outgoing, _query.Matches.Single().Hops.Single().Alias).ShouldEqual(("MEMBER_OF", true, "link"));
    [Fact] void should_read_the_condition_argument() => ((PathExpressionSyntax)_query.Conditions.Single().Value).Path.ShouldEqual("groupId");
    [Fact] void should_read_every_return() => _query.Returns.Select(item => item.Property).ShouldEqual(["personId", "person", "group", "since"]);
    [Fact] void should_be_valid() => Codes(Valid).ShouldBeEmpty();
    [Fact] void should_read_an_incoming_hop() => Compile(Valid.Replace("-link:MEMBER_OF->", "<-link:MEMBER_OF-").Replace("person:Person <-", "group:Group <-").Replace("group:Group\n", "person:Person\n")).Value!.Modules.Single().Features.Single().Slices.Single().GraphQueries.Single().Matches.Single().Hops.Single().Outgoing.ShouldBeFalse();
    [Fact] void should_be_observable_when_declared() => Compile(Valid.Replace("=> Membership[]", "=> observable Membership[]")).Value!.Modules.Single().Features.Single().Slices.Single().GraphQueries.Single().IsObservable.ShouldBeTrue();

    [Theory]
    [InlineData("query Q => Membership[] graph Organization\n  return\n    person = person.name\n")]
    [InlineData("query Q => Membership[] graph Organization\n  match person:Person -MEMBER_OF-> group:Group\n")]
    [InlineData("query Q => Membership[] graph Organization\n  match person:Person -MEMBER_OF- group:Group\n  return\n    person = person.name\n")]
    [InlineData("query Q => Membership[] graph Organization\n  optional match person:Person -MEMBER_OF-> group:Group\n  return\n    person = person.name\n")]
    [InlineData("query Q => Membership[] graph Organization\n  match person:Person -MEMBER_OF-> group:Group\n  return\n    person person.name\n")]
    [InlineData("query Q => Membership[] graph Organization\n  match person:Person -MEMBER_OF-> group:Group\n  where\n  return\n    person = person.name\n")]
    [InlineData("query Q => Membership[] graph Organization\n  nonsense\n")]
    public void should_report_a_malformed_query(string query) => Codes(query).ShouldContain(DiagnosticCodes.InvalidGraphQuery);

    [Theory]
    [InlineData("where group.groupId = $groupId or group.groupId = $other")]
    [InlineData("where group.groupId > $groupId")]
    public void should_refuse_what_this_version_does_not_have(string where) =>
        Codes(Valid.Replace("where group.groupId = $groupId", where)).ShouldContain(DiagnosticCodes.UnsupportedGraphQueryConstruct);

    [Fact] void should_refuse_an_unknown_graph() => Codes(Valid.Replace("graph Organization", "graph Nope")).ShouldContain(DiagnosticCodes.UnknownGraphQueryReference);
    [Fact] void should_refuse_an_unknown_label() => Codes(Valid.Replace("group:Group", "group:Nope")).ShouldContain(DiagnosticCodes.UnknownGraphQueryReference);
    [Fact] void should_refuse_an_unknown_edge() => Codes(Valid.Replace("link:MEMBER_OF", "link:NOPE")).ShouldContain(DiagnosticCodes.UnknownGraphQueryReference);
    [Fact] void should_refuse_an_unknown_property() => Codes(Valid.Replace("person.name", "person.nope")).ShouldContain(DiagnosticCodes.UnknownGraphQueryReference);
    [Fact] void should_refuse_an_unknown_alias() => Codes(Valid.Replace("person.name", "stranger.name")).ShouldContain(DiagnosticCodes.UnknownGraphQueryReference);
    [Fact] void should_refuse_an_edge_in_the_wrong_direction() => Codes(Valid.Replace("-link:MEMBER_OF->", "<-link:MEMBER_OF-")).ShouldContain(DiagnosticCodes.GraphQueryEdgeMismatch);
    [Fact] void should_refuse_a_duplicate_alias() => Codes(Valid.Replace("group:Group", "person:Group")).ShouldContain(DiagnosticCodes.DuplicateGraphQueryAlias);
    [Fact] void should_refuse_an_undeclared_argument() => Codes(Valid.Replace("$groupId", "$other")).ShouldContain(DiagnosticCodes.GraphQueryArgumentMismatch);
    [Fact] void should_refuse_an_argument_of_another_type() => Codes(Valid.Replace("filter groupId GroupId", "filter groupId String")).ShouldContain(DiagnosticCodes.GraphQueryArgumentMismatch);
    [Fact] void should_refuse_a_missing_required_return() => Codes(Valid.Replace("    person = person.name\n", string.Empty)).ShouldContain(DiagnosticCodes.GraphQueryReturnMismatch);
    [Fact] void should_refuse_a_return_the_read_model_lacks() => Codes(Valid + "    extra = person.name\n").ShouldContain(DiagnosticCodes.GraphQueryReturnMismatch);
    [Fact] void should_refuse_a_return_of_another_type() => Codes(Valid.Replace("person = person.name", "person = person.personId")).ShouldContain(DiagnosticCodes.GraphQueryReturnMismatch);

    [Fact]
    void should_refuse_an_optional_source_for_a_required_property() =>
        Codes(Valid.Replace("match person:Person -link:MEMBER_OF-> group:Group", "match person:Person\n  optional match person:Person -link:MEMBER_OF-> group:Group").Replace("group = group.groupId", "group = group.groupId"))
            .ShouldContain(DiagnosticCodes.GraphQueryOptionalReturn);

    [Fact]
    void should_accept_an_optional_source_for_an_optional_property() =>
        Codes(Valid.Replace("match person:Person -link:MEMBER_OF-> group:Group", "match person:Person\n  optional match person:Person -link:MEMBER_OF-> group:Group")
            .Replace("    person = person.name\n    group = group.groupId\n", "    person = person.name\n    group = person.personId\n").Replace("personId = person.personId", "personId = person.personId"))
            .ShouldNotContain(DiagnosticCodes.GraphQueryOptionalReturn);
}
