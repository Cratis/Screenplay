// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.given;

// The golden model's #211 slice carries every scoped projection shape; each spec replaces one part of it and validates.
public class a_scoped_projection_model : Specification
{
    protected SemanticApplication _application;
    protected SemanticSlice _slice;
    protected SemanticProjection _orders;
    protected SemanticProjection _lineLookup;
    protected SemanticReadModel _orderView;

    void Establish()
    {
        _application = canonical_serialization_golden_vectors.CreateSemanticModel().Application;
        _slice = _application.Modules.Single().Features.Single().Features.Single().Slices.Single(_ => _.Name == "ProjectionBlocks");
        _orders = _slice.Projections.Single(_ => _.Name == "OrderViewProjection");
        _lineLookup = _slice.Projections.Single(_ => _.Name == "LineLookupProjection");
        _orderView = _slice.ReadModels.Single(_ => _.Name == "OrderView");
    }

    protected SemanticProjectionScope OrdersScope => _orders.Scope!;

    protected SemanticProjectionChildren Lines => OrdersScope.Children.Single();

    protected SemanticId ReadModelProperty(string name) => _orderView.Properties.Single(_ => _.Name == name).Id;

    protected SemanticId EventProperty(string eventName, string name) =>
        _slice.Events.Single(_ => _.Name == eventName).Properties.Single(_ => _.Name == name).Id;

    protected SemanticId EventId(string name) => _slice.Events.Single(_ => _.Name == name).Id;

    protected SemanticId TypeProperty(string typeName, string name) =>
        _application.Types.Single(_ => _.Name == typeName).Properties.Single(_ => _.Name == name).Id;

    protected Exception ValidateOrders(SemanticProjectionScope scope) => Validate(_orders with { Scope = scope });

    protected Exception Validate(SemanticProjection replacement) => Catch.Exception(() => ExecutableSemanticModel.Create(
        LanguageVersion.V1,
        SemanticVersion.V1,
        Replace(replacement)));

    protected SemanticApplication Replace(SemanticProjection replacement)
    {
        var slice = _slice with { Projections = [.. _slice.Projections.Select(_ => _.Id == replacement.Id ? replacement : _)] };
        var nested = _application.Modules.Single().Features.Single().Features.Single();
        var replacedNested = nested with { Slices = [.. nested.Slices.Select(_ => _.Id == slice.Id ? slice : _)] };
        var feature = _application.Modules.Single().Features.Single() with { Features = [replacedNested] };
        return _application with { Modules = [_application.Modules.Single() with { Features = [feature] }] };
    }

    protected static SemanticProjectionMapping Map(SemanticProjectionOperation operation, SemanticProjectionValue? source, params SemanticId[] target) =>
        new([.. target], operation, source);
}
