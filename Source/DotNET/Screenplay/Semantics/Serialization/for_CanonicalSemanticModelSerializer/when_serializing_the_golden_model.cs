// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_golden_model : Specification
{
    byte[] _expected;
    byte[] _serialized;
    byte[] _reserialized;
    ExecutableSemanticModel _model;
    ExecutableSemanticModel _roundTripped;
    SemanticSpecificationError[] _errors;
    SemanticProducedEvent[] _produced;

    void Establish()
    {
        _expected = canonical_serialization_golden_vectors.SemanticModelBytes;
        _model = canonical_serialization_golden_vectors.CreateSemanticModel();
    }

    void Because()
    {
        _serialized = SemanticModelSerializer.Serialize(_model);
        _roundTripped = SemanticModelSerializer.Deserialize(_expected);
        _reserialized = SemanticModelSerializer.Serialize(_roundTripped);
        var slices = _roundTripped.Application.Modules.Single().Features.Single().Features.Single().Slices;
        _errors = [.. slices.SelectMany(_ => _.Specifications).SelectMany(_ => _.ThenErrors)];
        _produced = [.. slices.Single(_ => _.Name == "Creation").Commands.Single().Produces];
    }

    [Fact] void should_match_the_checked_in_utf8_bytes() => _serialized.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_warn_on_both_legacy_projection_transition_cardinalities() =>
        _model.DeprecationDiagnostics.Select(_ => (_.Code, _.Severity))
            .ShouldContainOnly([
                (DiagnosticCodes.DeprecatedProjectionTransitionCardinality, DiagnosticSeverity.Warning),
                (DiagnosticCodes.DeprecatedProjectionTransitionCardinality, DiagnosticSeverity.Warning)
            ]);
    [Fact] void should_include_the_event_contract_in_each_deprecation_diagnostic() =>
        _model.DeprecationDiagnostics.All(diagnostic => _model.Application.Modules.SelectMany(module => module.Features)
            .SelectMany(feature => feature.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Projections)
            .SelectMany(projection => projection.Transitions).Any(transition => diagnostic.Message.Contains(transition.EventContract.ToString(), StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_cache_the_deprecation_diagnostics() =>
        _model.DeprecationDiagnostics.ShouldEqual(_model.DeprecationDiagnostics);
    [Fact] void should_preserve_the_deprecation_diagnostic_after_round_trip() =>
        _roundTripped.DeprecationDiagnostics.Length.ShouldEqual(2);
    [Fact] void should_derive_flat_transition_instances_by_key() =>
        _roundTripped.Application.Modules.Single().Features.Single().Features.Single().Slices
            .Single(_ => _.Name == "EntitySummaries").Projections.Single().GetAffectedInstances()
            .All(_ => _.Match == SemanticAffectedProjectionMatch.OneByKey && _.FlatKey is not null).ShouldBeTrue();
    [Fact] void should_reserialize_the_golden_bytes_identically() => _reserialized.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_the_distinct_semantic_revision() => _roundTripped.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_cover_a_produced_event_without_a_condition_or_destination() => _produced.Any(_ => _.Condition is null && _.Destination is null).ShouldBeTrue();
    [Fact] void should_omit_when_for_the_read_only_specification()
    {
        using var document = JsonDocument.Parse(_serialized);
        var specifications = document.RootElement.GetProperty("application").GetProperty("modules")[0].GetProperty("features")[0]
            .GetProperty("features")[0].GetProperty("slices").EnumerateArray()
            .SelectMany(slice => slice.GetProperty("specifications").EnumerateArray())
            .ToArray();
        specifications.Single(_ => _.GetProperty("name").GetString() == "looks up established state without a command")
            .TryGetProperty("when", out _).ShouldBeFalse();
        specifications.Single(_ => _.GetProperty("name").GetString() == "creates an entity from existing state")
            .TryGetProperty("when", out _).ShouldBeTrue();
    }
    [Fact] void should_cover_a_bare_rejection() => _errors.Any(_ => _.Code is null && _.Message is null).ShouldBeTrue();
    [Fact] void should_cover_a_message_only_rejection() => _errors.Any(_ => _.Code is null && _.Message == "Title is invalid").ShouldBeTrue();
    [Fact] void should_cover_both_constraint_kinds() =>
        _roundTripped.Application.Modules.Single().Features.Single().Features.Single().Slices.SelectMany(_ => _.Constraints).Select(_ => _.Kind)
            .ShouldContainOnly([SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintKind.UniquePropertyValue]);
    [Fact] void should_keep_the_behavior_order() =>
        _roundTripped.Application.Modules.Single().Features.Single().Features.Single().Slices
            .Single(_ => _.Name == "EntitySummaries").Projections.Single().Transitions
            .Select(_ => _.AffectedInstance.Cardinality)
            .ShouldContainOnly([AffectedInstanceCardinality.ZeroOrOne, AffectedInstanceCardinality.One, AffectedInstanceCardinality.Many]);
}
#endif
