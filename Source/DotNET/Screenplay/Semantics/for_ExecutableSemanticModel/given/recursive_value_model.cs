// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.given;

public static class recursive_value_model
{
    static readonly SemanticId _nodeTypeId = Id(110);
    static readonly SemanticId _nextPropertyId = Id(111);

    public static SemanticValue CreateValue(int depth)
    {
        var value = SemanticValue.Composite([]);
        for (var level = 0; level < depth; level++)
        {
            value = SemanticValue.Composite([new(_nextPropertyId, value)]);
        }

        return value;
    }

    public static ExecutableSemanticModel Create(SemanticValue value)
    {
        var commandId = Id(120);
        var commandValuePropertyId = Id(121);
        var eventId = Id(130);
        var eventValuePropertyId = Id(131);
        var nodeType = new SemanticCompositeType(
            _nodeTypeId,
            "Node",
            [new(_nextPropertyId, "Next", SemanticTypeReference.ForCompositeType(_nodeTypeId, isOptional: true), false)]);
        var eventContract = new SemanticEventContract(
            eventId,
            EventContractId.CreateLegacy(ApplicationIdentity.Create("Recursive Values"), "NodeRecorded"),
            EventContractRevision.Initial,
            "NodeRecorded",
            [new(eventValuePropertyId, "Value", SemanticTypeReference.ForCompositeType(_nodeTypeId), false)]);
        var command = new SemanticCommand(
            commandId,
            "RecordNode",
            [new(commandValuePropertyId, "Value", SemanticTypeReference.ForCompositeType(_nodeTypeId), false)],
            [],
            [new(
                eventId,
                null,
                null,
                [new(eventValuePropertyId, SemanticExpression.Property(SemanticExpressionRootKind.Command, commandValuePropertyId))])]);
        var specification = new SemanticSpecification(
            Id(140),
            "records a recursive value",
            [],
            [],
            new(commandId, [new(commandValuePropertyId, value)]),
            [new(eventId, [new(eventValuePropertyId, value)])],
            [],
            [],
            []);
        var slice = new SemanticSlice(
            Id(150),
            "Recording",
            SemanticSliceKind.StateChange,
            [eventContract],
            [command],
            [],
            [],
            [],
            [specification]);
        var application = new SemanticApplication(
            Id(101),
            "Recursive Values",
            [],
            [nodeType],
            [new(Id(102), "Values", [new(Id(103), "Values", [], [slice])])]);
        return ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
    }

    static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value:x64}");
}
#endif
