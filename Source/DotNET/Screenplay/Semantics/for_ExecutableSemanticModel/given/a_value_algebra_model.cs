// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.given;

public class a_value_algebra_model : Semantics.given.a_valid_semantic_model
{
    protected SemanticId _backupsPropertyId;
    protected SemanticId _childNamePropertyId;
    protected SemanticId _childNotePropertyId;
    protected SemanticId _childTypeId;
    protected SemanticId _commandId;
    protected SemanticId _commandPayloadPropertyId;
    protected SemanticId _eventBackupsPropertyId;
    protected SemanticId _eventId;
    protected SemanticId _eventPayloadPropertyId;
    protected SemanticId _labelsPropertyId;
    protected SemanticId _payloadItemsPropertyId;
    protected SemanticId _payloadPreferredPropertyId;
    protected SemanticId _payloadTypeId;
    protected SemanticValue _validBackups;
    protected SemanticValue _validPayload;

    void Establish()
    {
        _childTypeId = Id(10);
        _payloadTypeId = Id(11);
        _childNamePropertyId = Id(20);
        _childNotePropertyId = Id(21);
        _payloadItemsPropertyId = Id(22);
        _payloadPreferredPropertyId = Id(23);
        _labelsPropertyId = Id(24);
        _commandId = Id(30);
        _commandPayloadPropertyId = Id(31);
        _backupsPropertyId = Id(32);
        _eventId = Id(40);
        _eventPayloadPropertyId = Id(41);
        _eventBackupsPropertyId = Id(42);

        var firstChild = SemanticValue.Composite(
        [
            new(_childNotePropertyId, SemanticValue.Null),
            new(_childNamePropertyId, SemanticValue.Text("first"))
        ]);
        var secondChild = SemanticValue.Composite(
        [
            new(_childNamePropertyId, SemanticValue.Text("second"))
        ]);
        _validPayload = SemanticValue.Composite(
        [
            new(_labelsPropertyId, SemanticValue.Array([])),
            new(_payloadItemsPropertyId, SemanticValue.Array([firstChild, secondChild])),
            new(_payloadPreferredPropertyId, secondChild)
        ]);
        _validBackups = SemanticValue.Array([]);
    }

    protected ExecutableSemanticModel CreateModel(
        SemanticValue payload,
        SemanticValue backups,
        SemanticExpression? payloadMapping = null,
        SemanticExpression? backupsMapping = null)
    {
        var childType = new SemanticCompositeType(
            _childTypeId,
            "Child",
            [
                new(_childNotePropertyId, "Note", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isOptional: true), false),
                new(_childNamePropertyId, "Name", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), false)
            ]);
        var payloadType = new SemanticCompositeType(
            _payloadTypeId,
            "Payload",
            [
                new(_payloadPreferredPropertyId, "Preferred", SemanticTypeReference.ForCompositeType(_childTypeId, isOptional: true), false),
                new(_labelsPropertyId, "Labels", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isCollection: true), false),
                new(_payloadItemsPropertyId, "Items", SemanticTypeReference.ForCompositeType(_childTypeId, isCollection: true), false)
            ]);
        var eventContract = new SemanticEventContract(
            _eventId,
            EventContractId.CreateLegacy(ApplicationIdentity.Create("Value Algebra"), "ValuesForwarded"),
            EventContractRevision.Initial,
            "ValuesForwarded",
            [
                new(_eventBackupsPropertyId, "Backups", SemanticTypeReference.ForCompositeType(_childTypeId, isCollection: true, isOptional: true), false),
                new(_eventPayloadPropertyId, "Payload", SemanticTypeReference.ForCompositeType(_payloadTypeId), false)
            ]);
        var command = new SemanticCommand(
            _commandId,
            "ForwardValues",
            [
                new(_backupsPropertyId, "Backups", SemanticTypeReference.ForCompositeType(_childTypeId, isCollection: true, isOptional: true), false),
                new(_commandPayloadPropertyId, "Payload", SemanticTypeReference.ForCompositeType(_payloadTypeId), false)
            ],
            [],
            [new(
                _eventId,
                null,
                null,
                [
                    new(
                        _eventPayloadPropertyId,
                        payloadMapping ?? SemanticExpression.Property(SemanticExpressionRootKind.Command, _commandPayloadPropertyId)),
                    new(
                        _eventBackupsPropertyId,
                        backupsMapping ?? SemanticExpression.Property(SemanticExpressionRootKind.Command, _backupsPropertyId))
                ])]);
        var commandValues = ImmutableArray.Create(
            new SemanticPropertyValue(_backupsPropertyId, backups),
            new SemanticPropertyValue(_commandPayloadPropertyId, payload));
        var eventValues = ImmutableArray.Create(
            new SemanticPropertyValue(_eventBackupsPropertyId, backups),
            new SemanticPropertyValue(_eventPayloadPropertyId, payload));
        var specification = new SemanticSpecification(
            Id(50),
            "forwards collection and composite values",
            [],
            [],
            new(_commandId, commandValues),
            [new(_eventId, eventValues)],
            [],
            [],
            []);
        var slice = new SemanticSlice(
            Id(60),
            "Forwarding",
            SemanticSliceKind.StateChange,
            [eventContract],
            [command],
            [],
            [],
            [],
            [specification]);
        var application = new SemanticApplication(
            Id(1),
            "Value Algebra",
            [],
            [payloadType, childType],
            [new(Id(2), "Values", [new(Id(3), "Values", [], [slice])])]);
        return ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
    }

    protected static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value:x64}");
}
#endif
