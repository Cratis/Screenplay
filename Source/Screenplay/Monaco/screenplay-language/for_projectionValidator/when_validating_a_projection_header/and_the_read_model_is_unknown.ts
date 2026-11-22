// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating a projection header and the read model is unknown', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection App.Orders => Unknown.Model\n  from Missing\n    missing = value'));
    });

    it('should report the model before body errors and skip unavailable schema checks', () => {
        markers.should.deep.equal([
            { severity: 8, startLineNumber: 1, startColumn: 26, endLineNumber: 1, endColumn: 39, message: "Read model 'Unknown.Model' not found" },
            { severity: 8, startLineNumber: 2, startColumn: 8, endLineNumber: 2, endColumn: 15, message: "Event type 'Missing' not found" },
        ]);
    });
});
