// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

// The scope member is written only when a projection carries one, so a flat projection keeps the bytes - and the revision -
// it had before the scoped shape existed.
public class when_round_tripping_projection_scopes : Specification
{
    static readonly string[] _projectionBlockTypes = ["OrderLine", "Shipping", "OrderLineKey"];
    ExecutableSemanticModel _model;
    ExecutableSemanticModel _roundTripped;
    byte[] _serialized;
    string _flatJson;
    SemanticProjection[] _projections;

    void Establish() => _model = canonical_serialization_golden_vectors.CreateSemanticModel();

    void Because()
    {
        _serialized = SemanticModelSerializer.Serialize(_model);
        _roundTripped = SemanticModelSerializer.Deserialize(_serialized);
        _projections = [.. _roundTripped.Application.Modules.Single().Features.Single().Features.Single().Slices.SelectMany(_ => _.Projections)];
        var flat = _model.Application with
        {
            Modules = [.. _model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Features = [.. feature.Features.Select(nested => nested with { Slices = [.. nested.Slices.Where(_ => _.Name != "ProjectionBlocks")] })]
                })]
            })],
            Types = [.. _model.Application.Types.Where(_ => !_projectionBlockTypes.Contains(_.Name))]
        };
        _flatJson = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, flat)));
    }

    [Fact] void should_reserialize_identically() => SemanticModelSerializer.Serialize(_roundTripped).SequenceEqual(_serialized).ShouldBeTrue();
    [Fact] void should_keep_the_revision() => _roundTripped.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_read_no_scope_for_a_flat_projection() => _projections.Single(_ => _.Name == "EntitySummaryProjection").Scope.ShouldBeNull();
    [Fact] void should_read_the_scope_of_a_scoped_projection() => _projections.Single(_ => _.Name == "OrderViewProjection").Scope!.Children.Single().Scope.From.Single().Mappings.Length.ShouldEqual(3);
    [Fact] void should_write_no_scope_member_for_flat_projections() => _flatJson.Contains("\"scope\"", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_write_composite_key_parts_in_property_identity_order() => CompositeParts.ShouldEqual([.. CompositeParts.Order(StringComparer.Ordinal)]);

    string[] CompositeParts => [.. ((SemanticProjectionCompositeKey)_projections.Single(_ => _.Name == "LineLookupProjection").Scope!.From.Single().Key).Parts.Select(_ => _.Property.ToString())];
}
