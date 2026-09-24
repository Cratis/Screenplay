// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_evaluating_a_nested_claim_target : a_valid_semantic_model
{
    SemanticExecutionResult _matching;
    SemanticExecutionResult _missing;

    void Because()
    {
        var type = _application.Types.Single();
        var member = type.Properties.Single();
        var slice = _application.Modules.Single().Features.Single().Slices.Single(value => value.Commands.Length > 0);
        var property = new SemanticProperty(Id(SemanticKind.Property, "RegisterProject.Metadata"), "Metadata", SemanticTypeReference.ForCompositeType(type.Id, isOptional: true), false);
        var command = slice.Commands.Single() with
        {
            Properties = [.. slice.Commands.Single().Properties, property],
            Authorization = new SemanticPolicyReference("MatchingName")
        };
        var application = ReplaceSlice(slice with { Commands = [command], Specifications = [] }) with
        {
            Policies = [new("MatchingName", new SemanticClaimCondition("name", SemanticClaimTargetKind.Artifact, "Metadata.DisplayName"))]
        };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application)).Plan!;
        var caller = new SemanticCaller(true, [], [new("NAME", "Screenplay")]);
        var request = SemanticExecutionRequest.Create(
            command.Id,
            [
                new(_commandProjectIdPropertyId, SemanticValue.Text("00000000-0000-0000-0000-000000000001")),
                new(_commandNamePropertyId, SemanticValue.Text("Screenplay")),
                new(property.Id, SemanticValue.Composite([new(member.Id, SemanticValue.Text("Screenplay"))]))
            ],
            []) with { Caller = caller };
        var evaluator = new SemanticEvaluator();
        _matching = evaluator.Execute(plan, SemanticWorld.Empty, request);
        _missing = evaluator.Execute(plan, SemanticWorld.Empty, request with
        {
            Values = [.. request.Values.Where(value => value.TargetProperty != property.Id), new(property.Id, SemanticValue.Null)]
        });
    }

    [Fact] void should_accept_the_nested_member_when_a_claim_matches() => _matching.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_an_absent_nested_target() => ((SemanticRejected)_missing).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
}
