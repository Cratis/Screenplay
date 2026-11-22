// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating event references and remove via join on reuses an event', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders\n  events Missing\n  remove via join on Missing\n  remove with Missing'));
    });

    it('should apply the same existence and duplicate rules to both removal forms', () => {
        markers.should.deep.equal([
            { severity: 8, startLineNumber: 2, startColumn: 10, endLineNumber: 2, endColumn: 17, message: "Event type 'Missing' not found" },
            { severity: 8, startLineNumber: 3, startColumn: 22, endLineNumber: 3, endColumn: 29, message: "Event type 'Missing' not found" },
            { severity: 8, startLineNumber: 3, startColumn: 22, endLineNumber: 3, endColumn: 29, message: "Duplicate event type 'Missing' - event types can only be used once at each level" },
            { severity: 8, startLineNumber: 4, startColumn: 15, endLineNumber: 4, endColumn: 22, message: "Event type 'Missing' not found" },
            { severity: 8, startLineNumber: 4, startColumn: 15, endLineNumber: 4, endColumn: 22, message: "Duplicate event type 'Missing' - event types can only be used once at each level" },
        ]);
    });
});
