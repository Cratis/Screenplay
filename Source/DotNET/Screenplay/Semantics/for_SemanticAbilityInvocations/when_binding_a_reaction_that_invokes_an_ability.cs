// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_binding_a_reaction_that_invokes_an_ability : Specification
{
    ExecutableSemanticModel _model;
    SemanticInvocation _invocation;

    void Because()
    {
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV51();
        _invocation = _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Reactions).SelectMany(reaction => reaction.Triggers).SelectMany(trigger => trigger.Invokes).Single();
    }

    [Fact] void should_select_v51() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V51);
    [Fact] void should_reference_the_system() => _invocation.System.ShouldEqual(_model.Application.Systems.Single().Id);
    [Fact] void should_name_the_ability() => _invocation.Ability.ShouldEqual("BookPickup");
    [Fact] void should_carry_no_command() => _invocation.Command.IsSet.ShouldBeFalse();
    [Fact] void should_map_the_inputs_by_name() => _invocation.Inputs.Select(input => input.Input).ShouldEqual(["parcelId"]);
    [Fact] void should_keep_the_branches_in_order() => _invocation.Branches.Select(branch => branch.Selector).ShouldEqual(["returned", "NoCapacity", "failed"]);
    [Fact] void should_read_the_returned_field_as_its_declared_type() => ((SemanticInvocationValueExpression)_invocation.Branches[0].Produces.Single().Mappings.Single().Source).Source.ShouldEqual("result");
    [Fact] void should_read_the_failure_reason_as_text() => ((SemanticInvocationValueExpression)_invocation.Branches[2].Produces.Single().Mappings.Single().Source).Member.ShouldEqual("reason");
}
#endif
