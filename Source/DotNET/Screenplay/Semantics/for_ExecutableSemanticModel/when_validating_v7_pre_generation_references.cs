// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_v7_pre_generation_references : a_v7_contract
{
    static SemanticApplication Authorized(SemanticPolicyCondition condition)
    {
        var application = Application(Command with { Authorization = new SemanticLogicalAuthorization(new SemanticPolicyReference("P"), SemanticLogicalOperator.And, new SemanticPolicyReference("Authenticated")) },
            Scenario with { GivenCaller = new(true, [], []) });
        return application with { Policies = [new("P", condition), new("Authenticated", new SemanticAuthenticatedCondition())] };
    }

    [Fact] void should_refuse_a_composed_policy_over_generated_values() => Refuse(Authorized(new SemanticLogicalPolicyCondition(new SemanticAuthenticatedCondition(), SemanticLogicalOperator.And, new SemanticClaimCondition("identity", SemanticClaimTargetKind.Artifact, "Id"))));
    [Fact] void should_refuse_a_negated_policy_over_generated_values() => Refuse(Authorized(new SemanticNotPolicyCondition(new SemanticClaimCondition("identity", SemanticClaimTargetKind.Artifact, "Id"))));
    [Fact] void should_refuse_a_negated_policy_over_a_generated_subject() => Refuse(Authorized(new SemanticNotPolicyCondition(new SemanticClaimCondition("identity", SemanticClaimTargetKind.Subject, null))));
    [Fact] void should_refuse_a_policy_over_a_generated_subject() => Refuse(Authorized(new SemanticClaimCondition("identity", SemanticClaimTargetKind.Subject, null)));
    [Fact]
    void should_allow_unrelated_and_input_only_policies()
    {
        Catch.Exception(() => Create(Authorized(new SemanticAuthenticatedCondition()))).ShouldBeNull();
        Catch.Exception(() => Create(Authorized(new SemanticClaimCondition("note", SemanticClaimTargetKind.Artifact, "Note")))).ShouldBeNull();
    }
    [Fact]
    void should_keep_opaque_policies_and_code_validation_as_contracts()
    {
        Catch.Exception(() => Create(Authorized(new SemanticOpaquePolicyCondition("opaque-policy")))).ShouldBeNull();
        Catch.Exception(() => Create(Application(Command with { CodeValidations = [new("opaque-validation")] }))).ShouldBeNull();
    }
    [Fact]
    void should_partition_reaction_mapping_targets()
    {
        var application = Application();
        var module = application.Modules[0];
        var feature = module.Features[0];
        var slice = feature.Slices[0];
        var reaction = new SemanticReaction(Id(10), "CreateOnStartup", [new(SemanticReactionTriggerKind.Startup) { Invokes = [new(CommandId, [])] }]);
        SemanticApplication With(SemanticReaction value) => application with { Modules = [module with { Features = [feature with { Slices = [slice with { Kind = SemanticSliceKind.Automation, Reactions = [value] }] }] }] };
        Catch.Exception(() => Create(With(reaction))).ShouldBeNull();
        Refuse(With(reaction with
        {
            Triggers = [reaction.Triggers[0] with { Invokes = [new(CommandId, [new(GeneratedId, SemanticExpression.FromValue(GeneratedValue))])] }]
        }));
        var requiredInput = Command.Properties[1] with { Type = NoteType with { IsOptional = false } };
        var requiredApplication = With(reaction);
        var requiredSlice = requiredApplication.Modules[0].Features[0].Slices[0] with { Commands = [Command with { Properties = [Command.Properties[0], requiredInput] }], Specifications = [] };
        Refuse(requiredApplication with { Modules = [module with { Features = [feature with { Slices = [requiredSlice] }] }] });
    }
    [Fact]
    void should_refuse_generation_outside_commands()
    {
        var application = Application();
        Refuse(application with { Types = [new(Id(10), "Type", [new(Id(11), "Generated", IdentityType, false) { IsGenerated = true }])] });
        Refuse(application with { Triggers = [new(Id(10), "Trigger", [new(Id(11), "Generated", IdentityType, false) { IsGenerated = true }])] });
        var module = application.Modules[0];
        var feature = module.Features[0];
        var slice = feature.Slices[0];
        var property = new SemanticProperty(Id(11), "Generated", IdentityType, false) { IsGenerated = true };
        Refuse(application with { Modules = [module with { Features = [feature with { Slices = [slice with { ReadModels = [new(Id(10), "ReadModel", [property])] }] }] }] });
        var contract = new SemanticEventContract(Id(10), EventContractId.CreateLegacy(ApplicationIdentity.Create("App"), "Created"), EventContractRevision.Initial, "Created", [property]);
        Refuse(application with { Modules = [module with { Features = [feature with { Slices = [slice with { Events = [contract] }] }] }] });
    }
}
