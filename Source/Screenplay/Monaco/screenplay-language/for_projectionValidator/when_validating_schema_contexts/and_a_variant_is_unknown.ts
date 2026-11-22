// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating schema contexts and a variant is unknown', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  variant Unknown\n    missing = value\n    from Known\n    enters on Known'));
    });

    it('should suppress schema warnings but retain variant-level duplicate checks', () => {
        markers.should.deep.equal([
            { severity: 8, startLineNumber: 2, startColumn: 11, endLineNumber: 2, endColumn: 18, message: "Read model 'Unknown' not found" },
            { severity: 8, startLineNumber: 5, startColumn: 15, endLineNumber: 5, endColumn: 20, message: "Duplicate event type 'Known' - event types can only be used once at each level" },
        ]);
    });
});
