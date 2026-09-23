// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// '$eventContext.<path>' reads are admitted from the shared EventContextCatalog (Syntax/EventContextCatalog.cs), which mirrors
// Chronicle's EventContext record (EventContext.cs:28-44). Only one portable scalar value binds, spelled canonically: collections,
// composites without a leaf, the derived function occurred.Week, paths through the optional causedBy.onBehalfOf and the enum
// observationState stay out - see SemanticEventContextScalars.
public class event_context_paths : for_SemanticModelBinder.given.a_semantic_binder
{
    static readonly Dictionary<SemanticPrimitiveType, string> _targets = new()
    {
        [SemanticPrimitiveType.Text] = "text",
        [SemanticPrimitiveType.WholeNumber] = "whole",
        [SemanticPrimitiveType.Boolean] = "flag",
        [SemanticPrimitiveType.DateTime] = "moment",
        [SemanticPrimitiveType.Uuid] = "identifier"
    };

    List<SemanticEventContextScalar> _scalars;
    List<string> _unbound;
    CompilationResult<SemanticCompilation> _pascalCased;
    CompilationResult<SemanticCompilation> _conceptValue;
    CompilationResult<SemanticCompilation> _eventSourceValue;
    CompilationResult<SemanticCompilation> _collection;
    CompilationResult<SemanticCompilation> _composite;
    CompilationResult<SemanticCompilation> _week;
    CompilationResult<SemanticCompilation> _onBehalfOf;
    CompilationResult<SemanticCompilation> _observationState;

    void Because()
    {
        _scalars =
        [
            .. EventContextCatalog.Paths
                .Select(path => SemanticEventContextScalars.Resolve(path.Path))
                .Where(scalar => scalar.Kind == SemanticEventContextScalarKind.Scalar)
                .DistinctBy(scalar => scalar.Path)
        ];
        _unbound = [.. _scalars.Where(scalar => Errors(BindMapping($"{_targets[scalar.Primitive]} = $eventContext.{scalar.Path}")).Any()).Select(scalar => scalar.Path)];
        _pascalCased = BindMapping("text = $eventContext.EventType.Id");
        _conceptValue = BindMapping("identifier = $eventContext.correlationId.value");
        _eventSourceValue = BindMapping("text = $eventContext.eventSourceId.value");
        _collection = BindMapping("text = $eventContext.tags");
        _composite = BindMapping("text = $eventContext.causedBy");
        _week = BindMapping("whole = $eventContext.occurred.Week");
        _onBehalfOf = BindMapping("text = $eventContext.causedBy.onBehalfOf.subject");
        _observationState = BindMapping("text = $eventContext.observationState");
    }

    [Fact] void should_find_the_scalar_paths_of_the_catalog() => _scalars.Count.ShouldEqual(17);
    [Fact] void should_bind_every_scalar_path_of_the_catalog() => _unbound.ShouldBeEmpty();
    [Fact] void should_admit_every_scalar_member_including_the_newer_ones() => SemanticEventContextScalars.All.ShouldContain("hash", "eventType.tombstone", "subjectIsEventSourceId", "eventSourceId");
    [Fact] void should_bind_a_pascal_cased_path_canonically() => Source(_pascalCased).ShouldEqual(SemanticProjectionValue.EventContext("eventType.id"));
    [Fact] void should_bind_the_value_of_a_concept_as_the_member_itself() => Source(_conceptValue).ShouldEqual(SemanticProjectionValue.EventContext("correlationId"));
    [Fact] void should_bind_the_event_source_value_as_the_event_source_identity() => Source(_eventSourceValue).ShouldEqual(SemanticProjectionValue.EventSourceIdentity);
    [Fact] void should_reject_a_collection() => Message(_collection).ShouldContain("collection");
    [Fact] void should_reject_a_composite_without_a_leaf() => Message(_composite).ShouldContain("name one of subject, name, userName, onBehalfOf");
    [Fact] void should_reject_the_derived_week() => Message(_week).ShouldContain("no derived-value shape");
    [Fact] void should_reject_a_path_through_on_behalf_of() => Message(_onBehalfOf).ShouldContain("'onBehalfOf' is optional");
    [Fact] void should_reject_the_observation_state() => Message(_observationState).ShouldContain("no portable primitive");

    CompilationResult<SemanticCompilation> BindMapping(string mapping) =>
        Bind($"""
            concept ActivityId : Uuid
            module Activity
              feature Tracking
                slice StateChange Events
                  event Happened
                    label String
                slice StateView Lookup
                  readmodel ActivityView
                    activityId ActivityId
                    text String?
                    whole Int?
                    flag Bool?
                    moment DateTime?
                    identifier Uuid?
                  projection Activity => ActivityView
                    from Happened
                      {mapping}
                  query ActivityById => ActivityView?
                    by activityId ActivityId
            """);

    static IEnumerable<Diagnostic> Errors(CompilationResult<SemanticCompilation> result) => result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error);

    static string Message(CompilationResult<SemanticCompilation> result) => string.Join('\n', Errors(result).Select(_ => _.Message));

    static SemanticProjectionValue Source(CompilationResult<SemanticCompilation> result) =>
        result.Value!.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.Projections).Single().Scope!.From.Single().Mappings.Single().Source!;
}
