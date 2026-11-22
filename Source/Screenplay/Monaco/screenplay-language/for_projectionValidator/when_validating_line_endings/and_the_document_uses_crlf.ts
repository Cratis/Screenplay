// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating line endings and the document uses CRLF', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('# comment\r\nprojection Orders => Root\r\n  missing = $eventContext.\r\n'));
    });

    it('should retain the same exact ranges as LF content', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 3, startColumn: 3, endLineNumber: 3, endColumn: 10, message: "Property 'missing' not found in Root" },
            { severity: 8, startLineNumber: 3, startColumn: 13, endLineNumber: 3, endColumn: 27, message: "'$eventContext.' names no member - expected one of eventType, eventSourceType, eventSourceId, eventStreamType, eventStreamId, sequenceNumber, occurred, eventStore, namespace, correlationId, causation, causedBy, tags, hash, observationState, subject, subjectIsEventSourceId.", code: 'PLAY0298' },
        ]);
    });
});
