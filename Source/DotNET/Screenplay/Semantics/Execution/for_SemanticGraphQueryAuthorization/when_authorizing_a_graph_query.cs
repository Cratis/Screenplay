// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGraphQueryAuthorization;

public class when_authorizing_a_graph_query : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Model =
        "command reads protected\n" +
        "concept PersonId : Uuid\nconcept GroupId : Uuid\n" +
        "policy GroupAdmin\n  require role \"GroupAdmin\"\n" +
        "module M\n  feature F\n    slice StateView S\n" +
        "      graph G\n        node Person\n          personId PersonId key\n        node Group\n          groupId GroupId key\n" +
        "        edge MEMBER_OF source Person target Group\n" +
        "      readmodel Membership\n        personId PersonId key\n" +
        "      query Open => Membership[] graph G\n        match person:Person -MEMBER_OF-> group:Group\n        return\n          personId = person.personId\n" +
        "      query Guarded => Membership[] graph G\n        authorize GroupAdmin\n        match person:Person -MEMBER_OF-> group:Group\n        return\n          personId = person.personId\n";

    SemanticExecutionPlan _plan;
    SemanticApplication _application;

    void Establish()
    {
        var result = Bind(Model);
        _application = result.Value!.Model.Application;
        _plan = SemanticExecutionPlan.Compile(result.Value.Model).Plan;
    }

    string? Refusal(string query, params string[] roles) =>
        SemanticGraphQueryAuthorization.Refusal(_application.GraphQueries.Single(candidate => candidate.Name == query), _plan, new(roles.Length > 0, [.. roles], []));

    [Fact] void should_let_anyone_run_a_query_without_authorization() => Refusal("Open").ShouldBeNull();
    [Fact] void should_let_a_caller_with_the_role_run_a_guarded_query() => Refusal("Guarded", "GroupAdmin").ShouldBeNull();
    [Fact] void should_refuse_a_caller_without_the_role() => Refusal("Guarded", "Other").ShouldEqual("Caller is not authorized.");
    [Fact] void should_refuse_an_anonymous_caller() => Refusal("Guarded").ShouldEqual("Caller is not authorized.");
}
