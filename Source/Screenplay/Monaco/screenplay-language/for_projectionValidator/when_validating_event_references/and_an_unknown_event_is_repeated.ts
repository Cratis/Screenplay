// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating event references and an unknown event is repeated', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders\n  from Missing-event.trailing\n  from Missing-event trailing'));
    });

    it('should report existence before duplication using the matched token range', () => {
        markers.should.deep.equal([
            { severity: 8, startLineNumber: 2, startColumn: 8, endLineNumber: 2, endColumn: 21, message: "Event type 'Missing-event' not found" },
            { severity: 8, startLineNumber: 3, startColumn: 8, endLineNumber: 3, endColumn: 21, message: "Event type 'Missing-event' not found" },
            { severity: 8, startLineNumber: 3, startColumn: 8, endLineNumber: 3, endColumn: 21, message: "Duplicate event type 'Missing-event' - event types can only be used once at each level" },
        ]);
    });
});
