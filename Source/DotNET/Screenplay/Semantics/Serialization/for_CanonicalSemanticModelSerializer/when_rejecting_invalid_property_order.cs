// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_rejecting_invalid_property_order : Specification
{
    SemanticApplication _application;
    SemanticModule _module;
    SemanticFeature _feature;
    SemanticSlice _slice;
    SemanticReadModel _model;

    void Establish()
    {
        _application = canonical_serialization_golden_vectors.CreateSemanticModelV37_1().Application;
        _module = _application.Modules[0];
        _feature = _module.Features[0];
        _slice = _feature.Slices.Single(slice => !slice.ReadModels.IsEmpty);
        _model = _slice.ReadModels.Single();
    }

    [Fact] void should_refuse_missing_property_identities() => Reject([_model.PropertyOrder[0]]);
    [Fact] void should_refuse_duplicate_property_identities() => Reject([.. Enumerable.Repeat(_model.PropertyOrder[0], _model.PropertyOrder.Length)]);
    [Fact] void should_refuse_foreign_property_identities() => Reject(_model.PropertyOrder.SetItem(0, _slice.Id));
    [Fact] void should_refuse_redundant_canonical_order() => Reject([.. _model.PropertyOrder.OrderBy(id => id.ToString(), StringComparer.Ordinal)]);

    void Reject(ImmutableArray<SemanticId> order)
    {
        var slice = _slice with { ReadModels = [_model with { PropertyOrder = order }] };
        var feature = _feature with { Slices = _feature.Slices.Replace(_slice, slice) };
        var module = _module with { Features = _module.Features.Replace(_feature, feature) };
        var application = _application with { Modules = _application.Modules.Replace(_module, module) };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V37_1, SemanticVersion.V37_1, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
