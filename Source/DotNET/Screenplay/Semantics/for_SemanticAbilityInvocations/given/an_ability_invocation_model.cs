// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations.given;

public class an_ability_invocation_model : Specification
{
    protected SemanticInvocation _invocation;
    protected Exception? _error;

    ExecutableSemanticModel _model;
    SemanticSlice _slice;

    void Establish()
    {
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV51();
        _slice = _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => slice.Name == "PickupBooking");
        _invocation = _slice.Reactions.Single().Triggers.Single().Invokes.Single();
    }

    protected Exception? Rebuild(params SemanticInvocation[] invocations)
    {
        var reaction = _slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var changed = _slice with { Reactions = [reaction with { Triggers = [trigger with { Invokes = [.. invocations] }] }] };
        var application = _model.Application with
        {
            Modules = [.. _model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice.Id == _slice.Id ? changed : slice)]
                })]
            })]
        };
        return Catch.Exception(() => ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application));
    }

    protected SemanticInvocation WithBranchValue(int index, SemanticInvocationValueExpression value)
    {
        var branch = _invocation.Branches[index];
        var produced = branch.Produces.Single();
        var changed = branch with { Produces = [produced with { Mappings = [produced.Mappings.Single() with { Source = value }] }] };
        return _invocation with { Branches = _invocation.Branches.SetItem(index, changed) };
    }
}
#endif
