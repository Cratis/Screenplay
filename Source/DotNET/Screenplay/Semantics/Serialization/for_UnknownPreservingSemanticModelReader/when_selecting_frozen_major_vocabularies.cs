// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_selecting_frozen_major_vocabularies : Specification
{
    bool[] _boundaries;
    bool _retiredField;

    void Because()
    {
        (uint Major, EsmBaselineShape Shape, string Name)[] introductions =
        [
            (6, EsmBaselineShape.Application, "triggers"),
            (8, EsmBaselineShape.Application, "eventSources"),
            (22, EsmBaselineShape.Application, "reactionProductionMode"),
            (25, EsmBaselineShape.Application, "commandReadMode"),
            (12, EsmBaselineShape.Concept, "numbers"),
            (13, EsmBaselineShape.Concept, "compliance"),
            (15, EsmBaselineShape.Concept, "generation"),
            (7, EsmBaselineShape.Property, "generated"),
            (16, EsmBaselineShape.EventContract, "storedName"),
            (9, EsmBaselineShape.EventContract, "visibility"),
            (9, EsmBaselineShape.EventContract, "origin"),
            (4, EsmBaselineShape.EventContract, "priorRevisions"),
            (4, EsmBaselineShape.EventContract, "predecessor"),
            (6, EsmBaselineShape.Slice, "reactions"),
            (6, EsmBaselineShape.Slice, "captures"),
            (9, EsmBaselineShape.Slice, "direction"),
            (7, EsmBaselineShape.Command, "response"),
            (8, EsmBaselineShape.Command, "route"),
            (23, EsmBaselineShape.Command, "handler"),
            (25, EsmBaselineShape.Command, "reads"),
            (6, EsmBaselineShape.ProducedEvent, "destinationType"),
            (10, EsmBaselineShape.ProducedEvent, "route"),
            (9, EsmBaselineShape.Projection, "event"),
            (9, EsmBaselineShape.Reducer, "event"),
            (10, EsmBaselineShape.Reducer, "from"),
            (14, EsmBaselineShape.Query, "filters"),
            (27, EsmBaselineShape.Query, "keyParts"),
            (17, EsmBaselineShape.Specification, "whenRedelivered"),
            (17, EsmBaselineShape.Specification, "thenNoEvents"),
            (5, EsmBaselineShape.Specification, "thenAbsentReadModels"),
            (7, EsmBaselineShape.Specification, "thenReturns"),
            (6, EsmBaselineShape.Specification, "givenClock"),
            (6, EsmBaselineShape.Specification, "givenCaptures"),
            (6, EsmBaselineShape.Specification, "whenClock"),
            (6, EsmBaselineShape.Specification, "whenTrigger"),
            (6, EsmBaselineShape.Specification, "whenCapture"),
            (30, EsmBaselineShape.Specification, "givenGenerated"),
            (34, EsmBaselineShape.Specification, "thenInvocations"),
            (8, EsmBaselineShape.SpecificationEvent, "route"),
            (8, EsmBaselineShape.SpecificationEvent, "unrouted"),
            (7, EsmBaselineShape.SpecificationCommand, "generatedValues"),
            (14, EsmBaselineShape.SpecificationQuery, "filters"),
            (25, EsmBaselineShape.Expression, "read"),
            (25, EsmBaselineShape.Expression, "property"),
            (25, EsmBaselineShape.ConditionOperand, "read"),
            (10, EsmBaselineShape.Reaction, "from"),
            (10, EsmBaselineShape.Reaction, "runsAs"),
            (19, EsmBaselineShape.Invocation, "onRefused"),
            (9, EsmBaselineShape.Capture, "sourceEvents"),
            (33, EsmBaselineShape.Capture, "keyIsEventSource")
        ];
        _boundaries = [.. introductions.Select(field => !EsmBaselineShapes.Fields(field.Major - 1, field.Shape).ContainsKey(field.Name) && EsmBaselineShapes.Fields(field.Major, field.Shape).ContainsKey(field.Name))];
        _retiredField = EsmBaselineShapes.Fields(3, EsmBaselineShape.AffectedInstance).ContainsKey("cardinality") && !EsmBaselineShapes.Fields(4, EsmBaselineShape.AffectedInstance).ContainsKey("cardinality");
    }

    [Fact] void should_freeze_each_introduction_at_its_own_major_not_at_the_latest_reader() => _boundaries.All(value => value).ShouldBeTrue();
    [Fact] void should_freeze_fields_removed_from_later_major_encodings() => _retiredField.ShouldBeTrue();
}
#endif
