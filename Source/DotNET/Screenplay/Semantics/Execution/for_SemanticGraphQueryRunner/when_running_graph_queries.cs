// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGraphQueryRunner;

public class when_running_graph_queries : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Kari = "11111111-1111-4111-8111-111111111111";
    const string Ola = "33333333-3333-4333-8333-333333333333";
    const string Ops = "22222222-2222-4222-8222-222222222222";
    const string Dev = "44444444-4444-4444-8444-444444444444";

    const string Model =
        "command reads protected\n" +
        "concept PersonId : Uuid\nconcept GroupId : Uuid\n" +
        "module M\n  feature F\n    slice StateView S\n" +
        "      event PersonRegistered\n        name String\n" +
        "      graph G\n        node Person\n          personId PersonId key\n          name String\n" +
        "        node Group\n          groupId GroupId key\n          title String\n" +
        "        edge MEMBER_OF source Person target Group\n          since String optional\n" +
        "        edge LEADS source Person target Group\n" +
        "      readmodel Membership\n        personId PersonId key\n        person String\n        title String\n        since String optional\n" +
        "      readmodel Leadership\n        groupId GroupId key\n        title String\n        leader String optional\n" +
        "      query MembersOf => Membership[] graph G\n        filter groupId GroupId\n" +
        "        match person:Person -link:MEMBER_OF-> group:Group\n        where group.groupId = $groupId\n" +
        "        return\n          personId = person.personId\n          person = person.name\n          title = group.title\n          since = link.since\n" +
        "      query Leaders => Leadership[] graph G\n" +
        "        match group:Group\n        optional match group:Group <-LEADS- leader:Person\n" +
        "        return\n          groupId = group.groupId\n          title = group.title\n          leader = leader.name\n" +
        "      query OpsMembers => Membership[] graph G\n" +
        "        match person:Person -link:MEMBER_OF-> group:Group\n        where group.title = \"Ops\"\n" +
        "        return\n          personId = person.personId\n          person = person.name\n          title = group.title\n          since = link.since\n";

    const string Seed =
        "        given graph G\n" +
        $"          has node Person \"{Kari}\"\n            name = \"Kari\"\n" +
        $"          has node Person \"{Ola}\"\n            name = \"Ola\"\n" +
        $"          has node Group \"{Ops}\"\n            title = \"Ops\"\n" +
        $"          has node Group \"{Dev}\"\n            title = \"Dev\"\n" +
        $"          has edge MEMBER_OF from \"{Kari}\" to \"{Ops}\"\n            since = \"2026\"\n" +
        $"          has edge MEMBER_OF from \"{Ola}\" to \"{Dev}\"\n" +
        $"          has edge LEADS from \"{Kari}\" to \"{Ops}\"\n";

    IReadOnlyList<SemanticGraphSpecificationResult> Run(string then)
    {
        var result = Bind($"{Model}      specification S\n{Seed}{then}");
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return SemanticGraphRunner.RunAll(result.Value!.Model);
    }

    static string Members(string group, string rows) => $"        then query MembersOf\n          arguments\n            groupId = \"{group}\"\n{rows}";

    [Fact] void should_pass_when_a_row_matches() => Run(Members(Ops, "          result\n            person = \"Kari\"\n            title = \"Ops\"\n")).Single().Passed.ShouldBeTrue();
    [Fact] void should_follow_the_edge_property() => Run(Members(Ops, "          result\n            since = \"2026\"\n")).Single().Passed.ShouldBeTrue();
    [Fact] void should_narrow_to_the_argument() => Run(Members(Dev, "          result\n            person = \"Ola\"\n")).Single().Passed.ShouldBeTrue();
    [Fact] void should_fail_when_a_row_is_missing() => Run(Members(Ops, "          result\n            person = \"Ola\"\n")).Single().Passed.ShouldBeFalse();
    [Fact] void should_fail_when_there_are_more_rows_than_expected() => Run(Members(Ops, string.Empty)).Single().Passed.ShouldBeFalse();
    [Fact] void should_pass_an_empty_result_for_a_group_nobody_is_in() => Run(Members("55555555-5555-4555-8555-555555555555", string.Empty)).Single().Passed.ShouldBeTrue();
    [Fact] void should_filter_on_a_literal() => Run("        then query OpsMembers\n          result\n            person = \"Kari\"\n").Single().Passed.ShouldBeTrue();

    [Fact]
    void should_keep_a_row_when_an_optional_match_finds_nothing() =>
        Run("        then query Leaders\n          result\n            title = \"Ops\"\n            leader = \"Kari\"\n          result\n            title = \"Dev\"\n").Single().Passed.ShouldBeTrue();
}
