// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_concept_code_validation : given.a_semantic_binder
{
    const string Source =
        """
        concept Label : String
          validate
            ```csharp
            yield return "Invalid label";
            ```
        module Orders
          feature Ordering
            slice StateChange Orders
              command PlaceOrder
                label Label
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_activate_v3_without_a_reducer() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V3);
    [Fact] void should_bind_the_concept_value() => Rule.Property.IsSet.ShouldBeFalse();
    [Fact] void should_preserve_the_message_yielding_contract() => Rule.Kind.ShouldEqual(SemanticValidationRuleKind.CodeValidation);
    [Fact] void should_reference_the_concept_validation_attachment() => Rule.RequirementId.ShouldEqual(_result.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_keep_the_canonical_member() => _result.ImplementationRequirements.Single().Member.ShouldEqual("code validation 0");
    [Fact] void should_name_the_owner_in_the_human_facing_rule() => Rule.Name.ShouldEqual("Concept 'Label' code validation 0");
    [Fact] void should_admit_a_reference_plan() => SemanticExecutionPlan.Compile(_result.Value!.Model).Success.ShouldBeTrue();
    [Fact] void should_round_trip_the_concept_code_contract() => Serialization.SemanticModelSerializer.Deserialize(Serialization.SemanticModelSerializer.Serialize(_result.Value!.Model)).Application.Concepts.Single().Validations.Single().Kind.ShouldEqual(SemanticValidationRuleKind.CodeValidation);
    [Fact] void should_require_pure_capability() => _result.ImplementationRequirements.Single().RequiredCapability.ShouldEqual("pure");
    [Fact] void should_report_unsupported_instead_of_accepting()
    {
        var plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
        var command = plan.Commands.Values.Single();
        var request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text("valid"))], []);
        var execution = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        ((SemanticUnsupported)execution).Details.ShouldEqual("Concept 'Label' code validation 0 has an opaque validation predicate and requires a target provider.");
    }

    SemanticValidationRule Rule => _result.Value!.Model.Application.Concepts.Single().Validations.Single();
}
