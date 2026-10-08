// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PolicyNegationCorpus;

public class when_executing_composed_negation_policies : Specification
{
    ExecutableSemanticModel _model;

    void Establish() => _model = SemanticModelSerializer.Deserialize(PolicyNegationCorpus.V7.EsmBytes.AsSpan());

    [Fact] void should_allow_a_person_through_the_grouped_policy() => Evaluate("Grouped", new(true, ["Accountant"], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_a_service_role_through_the_grouped_policy() => Denied("Grouped", new(true, ["Service"], []));
    [Fact] void should_deny_a_service_claim_through_the_grouped_policy() => Denied("Grouped", new(true, [], [new("actorKind", "service")]));
    [Fact] void should_deny_an_unauthenticated_person_through_the_grouped_policy() => Denied("Grouped", new(false, ["Accountant"], []));
    [Fact] void should_allow_an_accountant_through_the_precedence_policy() => Evaluate("Precedence", new(true, ["Accountant"], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_allow_a_controller_despite_a_service_role_through_the_precedence_policy() => Evaluate("Precedence", new(false, ["Service", "Controller"], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_a_service_accountant_through_the_precedence_policy() => Denied("Precedence", new(true, ["Service", "Accountant"], []));
    [Fact] void should_allow_an_authenticated_caller_through_the_double_negative_policy() => Evaluate("DoubleNegative", new(true, [], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_an_unauthenticated_caller_through_the_double_negative_policy() => Denied("DoubleNegative", new(false, [], []));

    SemanticExecutionResult Evaluate(string policy, SemanticCaller caller)
    {
        // Exercise the corpus's actual policy conditions through a command, without changing its frozen vector.
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single() with { Authorization = new SemanticPolicyReference(policy) };
        var application = _model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command] }] }] }]
        };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, application);
        var request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text(PolicyNegationCorpus.V7.RuntimeStreamId))], []) with { Caller = caller };

        return new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, request);
    }

    void Denied(string policy, SemanticCaller caller) => ((SemanticRejected)Evaluate(policy, caller)).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
}
