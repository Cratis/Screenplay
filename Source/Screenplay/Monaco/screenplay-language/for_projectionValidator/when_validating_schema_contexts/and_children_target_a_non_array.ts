// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating schema contexts and children target a non-array', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  children scalar identified by id\n    missing = value'));
    });

    it('should report the non-array target and retain the root schema for descendants', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 2, startColumn: 12, endLineNumber: 2, endColumn: 18, message: "Property 'scalar' is not a collection (array) type" },
            { severity: 4, startLineNumber: 3, startColumn: 5, endLineNumber: 3, endColumn: 12, message: "Property 'missing' not found in Root" },
        ]);
    });
});
