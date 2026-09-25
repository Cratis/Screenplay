// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalVectors.Specs.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelSerializer;

public class when_round_tripping_the_v4_golden_bytes : Specification
{
    byte[] _actual = [];
    byte[] _expected = [];

    void Establish() => _expected = canonical_serialization_golden_bytes.SemanticModelV4;

    void Because() => _actual = SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_expected));

    [Fact] void should_preserve_the_exact_v4_bytes() => _actual.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_pin_the_reviewed_golden_revision() => SemanticModelSerializer.Deserialize(_expected).Revision.ToString().ShouldEqual("rev1:8e7c31552c528d080a8617d1fe44b6a1dff4a636d98a00709baa93ba985283b2");
    [Fact] void should_allow_v2_and_v3_constructs_in_v4()
    {
        var model = SemanticModelSerializer.Deserialize(_expected);
        model.Application.Modules.Single().Features.Single().Features.Single().Slices.SelectMany(slice => slice.Commands)
            .Any(command => command.Destination is not null).ShouldBeTrue();
        var application = model.Application with
        {
            Policies = model.Application.Policies.Add(new("OpaqueRule", new SemanticOpaquePolicyCondition(new string('a', 64))))
        };
        ExecutableSemanticModel.Create(LanguageVersion.V4, SemanticVersion.V4, application)
            .SemanticVersion.ShouldEqual(SemanticVersion.V4);
    }

    [Fact] void should_reject_v4_without_lineage()
    {
        var model = SemanticModelSerializer.Deserialize(_expected);
        var module = model.Application.Modules.Single();
        var root = module.Features.Single();
        var feature = root.Features.Single();
        var app = model.Application with
        {
            Modules = [module with { Features = [root with { Features = [feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Events = [.. slice.Events.Select(@event => @event with
                    {
                        Revision = EventContractRevision.Initial,
                        Predecessor = null,
                        PriorRevisions = []
                    })]
                })]
            }] }] }]
        };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V4, SemanticVersion.V4, app))
            .ShouldBeOfExactType<InvalidSemanticContract>();
    }
    [Fact] void should_omit_transition_cardinality()
    {
        var json = System.Text.Encoding.UTF8.GetString(_actual);
        json.ShouldContain("\"affectedInstance\":{\"key\"");
        json.ShouldNotContain("\"affectedInstance\":{\"cardinality\"");
    }

    [Theory]
    [InlineData("many")]
    [InlineData("zeroOrOne")]
    [InlineData("one")]
    void should_reject_a_v4_reader_with_legacy_transition_cardinality(string cardinality)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(_expected)
            .Replace("\"affectedInstance\":{\"key\"", $"\"affectedInstance\":{{\"cardinality\":\"{cardinality}\",\"key\"", StringComparison.Ordinal));
        Catch.Exception(() => SemanticModelSerializer.Deserialize(bytes)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Theory]
    [InlineData(AffectedInstanceCardinality.ZeroOrOne)]
    [InlineData(AffectedInstanceCardinality.Many)]
    void should_reject_legacy_transition_cardinality_in_v4(AffectedInstanceCardinality cardinality)
    {
        var model = SemanticModelSerializer.Deserialize(_expected);
        var module = model.Application.Modules.Single();
        var root = module.Features.Single();
        var feature = root.Features.Single();
        var projectionSlice = feature.Slices.Single(slice => slice.Projections.Any(value => !value.Transitions.IsEmpty));
        var projection = projectionSlice.Projections.Single(value => !value.Transitions.IsEmpty);
        var transition = projection.Transitions.Single() with
        {
            AffectedInstance = projection.Transitions.Single().AffectedInstance with { Cardinality = cardinality }
        };
        var changed = model.Application with
        {
            Modules = [module with { Features = [root with { Features = [feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice == projectionSlice ? slice with
                {
                    Projections = [projection with { Scope = null, Transitions = [transition] }]
                } : slice)]
            }] }] }]
        };
        var error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V4, SemanticVersion.V4, changed));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("v4");
    }
}
