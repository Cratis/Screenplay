// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_the_context_is_projected : given.a_policy
{
    void Establish() => Request = Request with
    {
        Caller = new(true, ["Member", "Member"], [new(ClaimTypes.Role.ToUpperInvariant(), "Owner"), new("region", "North"), new("REGION", "South")])
    };

    void Because()
    {
        Result = Execute();
        new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, QueryRequest()).ShouldBeOfExactType<SemanticAccepted>();
    }

    [Fact] void should_project_the_command_property_identity() => Contexts[0].Artifact.Properties.Single().TargetProperty.ShouldEqual(Plan.Commands.Values.Single().Properties.Single().Id);
    [Fact] void should_project_the_query_argument_identity() => Contexts[1].Artifact.Properties.Single().TargetProperty.ShouldEqual(Plan.Queries.Values.Single().Argument!.Id);
    [Fact] void should_project_subject_text() => Contexts.Select(context => context.Subject).ShouldEqual(new[] { "First", "First" });
    [Fact] void should_project_known_tenant_and_time() => Contexts.All(context => context.Tenant == "tenant" && context.Occurred == Request.Occurrence!.Occurred).ShouldBeTrue();
    [Fact] void should_preserve_role_order_and_duplicates() => Contexts[0].Identity.Roles.ShouldEqual(new[] { "Member", "Member", "Owner" });
    [Fact] void should_use_ordinal_role_comparison() => Contexts[0].Identity.HasRole("member").ShouldBeFalse();
    [Fact] void should_normalize_role_claims_out_of_ordinary_claims() => Contexts[0].Identity.Claims.Length.ShouldEqual(2);
    [Fact] void should_compare_claim_names_without_case_and_preserve_values() => Contexts[0].Identity.ClaimValues("Region").ShouldEqual(new[] { "North", "South" });
    [Fact] void should_find_claim_names_without_case() => Contexts[0].Identity.HasClaim("REGION").ShouldBeTrue();
    [Fact] void should_return_null_for_a_missing_claim() => Contexts[0].Identity.ClaimValue("missing").ShouldBeNull();
    [Fact] void should_leave_caller_owned_provider_disposal_to_the_host() => Provider.Disposals.ShouldEqual(0);
    [Fact] void should_choose_the_first_claim_value() => Contexts[0].Identity.ClaimValue("REGION").ShouldEqual("North");
    [Fact] void should_preserve_authentication() => Contexts[0].Identity.IsAuthenticated.ShouldBeTrue();

}
