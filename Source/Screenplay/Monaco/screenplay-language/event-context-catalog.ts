// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Generated from Cratis.Screenplay.Syntax.EventContextCatalog - do not edit by hand. The spec
// Syntax/for_EventContextCatalog/when_holding_the_editor_catalog_to_it fails when this file drifts; rerun
// it with SCREENPLAY_REGENERATE_EVENT_CONTEXT=1 to rewrite it.

export type EventContextMemberKind = 'value' | 'composite' | 'collection' | 'function';

export interface EventContextMember {
    readonly name: string;
    readonly type: string;
    readonly kind: EventContextMemberKind;
    readonly description: string;
}

export interface EventContextPath extends EventContextMember {
    readonly path: string;
}

export const eventContextRootType = 'EventContext';

// The members of each type a path passes through, keyed by type name.
export const eventContextTypes: Readonly<Record<string, readonly EventContextMember[]>> = {
    'EventContext': [
        { name: 'eventType', type: 'EventType', kind: 'composite', description: 'The type of the event - its identifier, generation and whether it is a tombstone.' },
        { name: 'eventSourceType', type: 'EventSourceType', kind: 'value', description: 'The type of the event source the event was appended to.' },
        { name: 'eventSourceId', type: 'EventSourceId', kind: 'value', description: 'The identifier of the event source the event was appended to - the value $eventSourceId resolves.' },
        { name: 'eventStreamType', type: 'EventStreamType', kind: 'value', description: 'The type of the event stream the event belongs to.' },
        { name: 'eventStreamId', type: 'EventStreamId', kind: 'value', description: 'The identifier of the event stream the event belongs to.' },
        { name: 'sequenceNumber', type: 'EventSequenceNumber', kind: 'value', description: 'The position of the event in its event sequence.' },
        { name: 'occurred', type: 'DateTimeOffset', kind: 'value', description: 'When the event occurred - the time it was appended unless one was given.' },
        { name: 'eventStore', type: 'EventStoreName', kind: 'value', description: 'The name of the event store the event belongs to.' },
        { name: 'namespace', type: 'EventStoreNamespaceName', kind: 'value', description: 'The namespace within the event store the event belongs to.' },
        { name: 'correlationId', type: 'CorrelationId', kind: 'value', description: 'The correlation identifier the event was appended with.' },
        { name: 'causation', type: 'IEnumerable<Causation>', kind: 'collection', description: 'The chain of causes that led to the event - a collection, not addressable below.' },
        { name: 'causedBy', type: 'Identity', kind: 'composite', description: 'The identity that caused the event.' },
        { name: 'tags', type: 'IEnumerable<Tag>', kind: 'collection', description: 'The tags the event was appended with - a collection, not addressable below.' },
        { name: 'hash', type: 'EventHash', kind: 'value', description: 'The hash of the event content.' },
        { name: 'observationState', type: 'EventObservationState', kind: 'value', description: 'Whether the observer sees the event for the first time or again on replay - differs between live processing and replay.' },
        { name: 'subject', type: 'Subject', kind: 'value', description: 'The compliance subject the event is about - the event source identifier unless one was given.' },
        { name: 'subjectIsEventSourceId', type: 'bool', kind: 'value', description: 'Whether the subject is the event source identifier.' },
    ],
    'EventType': [
        { name: 'id', type: 'EventTypeId', kind: 'value', description: 'The identifier of the event type.' },
        { name: 'generation', type: 'EventTypeGeneration', kind: 'value', description: 'The generation of the event type.' },
        { name: 'tombstone', type: 'bool', kind: 'value', description: 'Whether the event is a tombstone event.' },
    ],
    'EventSourceType': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'EventSourceId': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'EventStreamType': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'EventStreamId': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'EventSequenceNumber': [
        { name: 'value', type: 'ulong', kind: 'value', description: 'The underlying ulong value.' },
    ],
    'DateTimeOffset': [
        { name: 'Week', type: 'int', kind: 'function', description: 'The ISO 8601 week of the year the value falls in.' },
    ],
    'EventStoreName': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'EventStoreNamespaceName': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'CorrelationId': [
        { name: 'value', type: 'Guid', kind: 'value', description: 'The underlying Guid value.' },
    ],
    'Identity': [
        { name: 'subject', type: 'string', kind: 'value', description: 'The subject identifier of the identity.' },
        { name: 'name', type: 'string', kind: 'value', description: 'The display name of the identity.' },
        { name: 'userName', type: 'string', kind: 'value', description: 'The user name of the identity.' },
        { name: 'onBehalfOf', type: 'Identity', kind: 'composite', description: 'The identity this identity acted on behalf of, if any - an identity with the same members.' },
    ],
    'EventHash': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'Subject': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'EventTypeId': [
        { name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    ],
    'EventTypeGeneration': [
        { name: 'value', type: 'uint', kind: 'value', description: 'The underlying uint value.' },
    ],
};

// Every path the catalog lists, depth first in declaration order.
export const eventContextPaths: readonly EventContextPath[] = [
    { path: 'eventType', name: 'eventType', type: 'EventType', kind: 'composite', description: 'The type of the event - its identifier, generation and whether it is a tombstone.' },
    { path: 'eventType.id', name: 'id', type: 'EventTypeId', kind: 'value', description: 'The identifier of the event type.' },
    { path: 'eventType.id.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'eventType.generation', name: 'generation', type: 'EventTypeGeneration', kind: 'value', description: 'The generation of the event type.' },
    { path: 'eventType.generation.value', name: 'value', type: 'uint', kind: 'value', description: 'The underlying uint value.' },
    { path: 'eventType.tombstone', name: 'tombstone', type: 'bool', kind: 'value', description: 'Whether the event is a tombstone event.' },
    { path: 'eventSourceType', name: 'eventSourceType', type: 'EventSourceType', kind: 'value', description: 'The type of the event source the event was appended to.' },
    { path: 'eventSourceType.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'eventSourceId', name: 'eventSourceId', type: 'EventSourceId', kind: 'value', description: 'The identifier of the event source the event was appended to - the value $eventSourceId resolves.' },
    { path: 'eventSourceId.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'eventStreamType', name: 'eventStreamType', type: 'EventStreamType', kind: 'value', description: 'The type of the event stream the event belongs to.' },
    { path: 'eventStreamType.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'eventStreamId', name: 'eventStreamId', type: 'EventStreamId', kind: 'value', description: 'The identifier of the event stream the event belongs to.' },
    { path: 'eventStreamId.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'sequenceNumber', name: 'sequenceNumber', type: 'EventSequenceNumber', kind: 'value', description: 'The position of the event in its event sequence.' },
    { path: 'sequenceNumber.value', name: 'value', type: 'ulong', kind: 'value', description: 'The underlying ulong value.' },
    { path: 'occurred', name: 'occurred', type: 'DateTimeOffset', kind: 'value', description: 'When the event occurred - the time it was appended unless one was given.' },
    { path: 'occurred.Week', name: 'Week', type: 'int', kind: 'function', description: 'The ISO 8601 week of the year the value falls in.' },
    { path: 'eventStore', name: 'eventStore', type: 'EventStoreName', kind: 'value', description: 'The name of the event store the event belongs to.' },
    { path: 'eventStore.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'namespace', name: 'namespace', type: 'EventStoreNamespaceName', kind: 'value', description: 'The namespace within the event store the event belongs to.' },
    { path: 'namespace.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'correlationId', name: 'correlationId', type: 'CorrelationId', kind: 'value', description: 'The correlation identifier the event was appended with.' },
    { path: 'correlationId.value', name: 'value', type: 'Guid', kind: 'value', description: 'The underlying Guid value.' },
    { path: 'causation', name: 'causation', type: 'IEnumerable<Causation>', kind: 'collection', description: 'The chain of causes that led to the event - a collection, not addressable below.' },
    { path: 'causedBy', name: 'causedBy', type: 'Identity', kind: 'composite', description: 'The identity that caused the event.' },
    { path: 'causedBy.subject', name: 'subject', type: 'string', kind: 'value', description: 'The subject identifier of the identity.' },
    { path: 'causedBy.name', name: 'name', type: 'string', kind: 'value', description: 'The display name of the identity.' },
    { path: 'causedBy.userName', name: 'userName', type: 'string', kind: 'value', description: 'The user name of the identity.' },
    { path: 'causedBy.onBehalfOf', name: 'onBehalfOf', type: 'Identity', kind: 'composite', description: 'The identity this identity acted on behalf of, if any - an identity with the same members.' },
    { path: 'tags', name: 'tags', type: 'IEnumerable<Tag>', kind: 'collection', description: 'The tags the event was appended with - a collection, not addressable below.' },
    { path: 'hash', name: 'hash', type: 'EventHash', kind: 'value', description: 'The hash of the event content.' },
    { path: 'hash.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'observationState', name: 'observationState', type: 'EventObservationState', kind: 'value', description: 'Whether the observer sees the event for the first time or again on replay - differs between live processing and replay.' },
    { path: 'subject', name: 'subject', type: 'Subject', kind: 'value', description: 'The compliance subject the event is about - the event source identifier unless one was given.' },
    { path: 'subject.value', name: 'value', type: 'string', kind: 'value', description: 'The underlying string value.' },
    { path: 'subjectIsEventSourceId', name: 'subjectIsEventSourceId', type: 'bool', kind: 'value', description: 'Whether the subject is the event source identifier.' },
];
