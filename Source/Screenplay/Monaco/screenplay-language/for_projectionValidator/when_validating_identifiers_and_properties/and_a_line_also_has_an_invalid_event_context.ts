// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating identifiers and properties and a line also has an invalid event context', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  missing = $eventContext.\n  key Missing\n  another = $eventContext.invalid\n  key Another\n  identified by absent $eventContext.'));
    });

    it('should append all shared event-context issues after all local warnings', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 2, startColumn: 3, endLineNumber: 2, endColumn: 10, message: "Property 'missing' not found in Root" },
            { severity: 4, startLineNumber: 3, startColumn: 7, endLineNumber: 3, endColumn: 14, message: "Composite key type 'Missing' not found in read model schema" },
            { severity: 4, startLineNumber: 4, startColumn: 3, endLineNumber: 4, endColumn: 10, message: "Property 'another' not found in Root" },
            { severity: 4, startLineNumber: 5, startColumn: 7, endLineNumber: 5, endColumn: 14, message: "Composite key type 'Another' not found in read model schema" },
            { severity: 8, startLineNumber: 2, startColumn: 13, endLineNumber: 2, endColumn: 27, message: "'$eventContext.' names no member - expected one of eventType, eventSourceType, eventSourceId, eventStreamType, eventStreamId, sequenceNumber, occurred, eventStore, namespace, correlationId, causation, causedBy, tags, hash, observationState, subject, subjectIsEventSourceId.", code: 'PLAY0298' },
            { severity: 4, startLineNumber: 4, startColumn: 13, endLineNumber: 4, endColumn: 34, message: "Unknown event context member 'invalid' in '$eventContext.invalid' - expected one of eventType, eventSourceType, eventSourceId, eventStreamType, eventStreamId, sequenceNumber, occurred, eventStore, namespace, correlationId, causation, causedBy, tags, hash, observationState, subject, subjectIsEventSourceId.", code: 'PLAY0295' },
            { severity: 8, startLineNumber: 6, startColumn: 24, endLineNumber: 6, endColumn: 38, message: "'$eventContext.' names no member - expected one of eventType, eventSourceType, eventSourceId, eventStreamType, eventStreamId, sequenceNumber, occurred, eventStore, namespace, correlationId, causation, causedBy, tags, hash, observationState, subject, subjectIsEventSourceId.", code: 'PLAY0298' },
        ]);
    });
});
