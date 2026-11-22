// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating identifiers and properties and a composite type is missing', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  key Missing\n  key Composite\n  key id\n  key $eventSourceId'));
    });

    it('should warn only for the missing schema definition', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 2, startColumn: 7, endLineNumber: 2, endColumn: 14, message: "Composite key type 'Missing' not found in read model schema" },
        ]);
    });
});
