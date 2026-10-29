// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_capabilities_are_role_specific : given.a_command_handler
{
    [Fact]
    void should_resolve_and_execute_a_provider_defined_handler()
    {
        var body = Bodies.Single();
        body.Requirement.RequiredCapability.ShouldEqual("provider-defined");
        new SemanticRuleBodyExecution(Provider, Bodies).FindBody(body.Requirement.RequirementId, SemanticImplementationRole.CommandHandler).ShouldEqual(body);
        Execute().ShouldBeOfExactType<SemanticAccepted>();
    }

    [Fact]
    void should_not_admit_a_handler_relabelled_as_pure()
    {
        var body = Bodies.Single();
        var relabelled = body with { Requirement = body.Requirement with { RequiredCapability = "pure" } };
        new SemanticRuleBodyExecution(Provider, [relabelled]).FindBody(body.Requirement.RequirementId, SemanticImplementationRole.CommandHandler).ShouldBeNull();
        var calls = Provider.HandleCalls;
        new SemanticEvaluator(Provider, [relabelled]).Execute(Plan, SemanticWorld.Empty, Request).ShouldBeOfExactType<SemanticUnsupported>();
        Provider.HandleCalls.ShouldEqual(calls);
    }

    [Theory]
    [InlineData(SemanticImplementationRole.RulePredicate)]
    [InlineData(SemanticImplementationRole.CommandValidation)]
    [InlineData(SemanticImplementationRole.ConceptValidation)]
    [InlineData(SemanticImplementationRole.PolicyPredicate)]
    [InlineData(SemanticImplementationRole.ReducerTransition)]
    void should_keep_non_handler_roles_pure(SemanticImplementationRole role)
    {
        var body = Bodies.Single() with { Requirement = Bodies.Single().Requirement with { Role = role, RequiredCapability = "pure" } };
        new SemanticRuleBodyExecution(Provider, [body]).FindBody(body.Requirement.RequirementId, role).ShouldEqual(body);
        var impure = body with { Requirement = body.Requirement with { RequiredCapability = "provider-defined" } };
        new SemanticRuleBodyExecution(Provider, [impure]).FindBody(body.Requirement.RequirementId, role).ShouldBeNull();
        var unknown = body with { Requirement = body.Requirement with { RequiredCapability = "unknown" } };
        new SemanticRuleBodyExecution(Provider, [unknown]).FindBody(body.Requirement.RequirementId, role).ShouldBeNull();
    }
}
