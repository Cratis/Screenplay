// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating schema contexts and children items have no array type', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  children loose identified by id\n    childOnly = value\n    missing = value'));
    });

    it('should accept the items schema without requiring an array type', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 4, startColumn: 5, endLineNumber: 4, endColumn: 12, message: "Property 'missing' not found in LooseChild" },
        ]);
    });
});
