// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating identifiers and properties and assignment and aggregate targets are missing', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  missing = value\n  increment missing\n  decrement missing\n  count missing\n  add missing by 1\n  subtract missing by 1\n  total = value\n  count total\n  missing.path = value\n  count missing.path'));
    });

    it('should preserve all aggregate and assignment target warnings in source order', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 2, startColumn: 3, endLineNumber: 2, endColumn: 10, message: "Property 'missing' not found in Root" },
            { severity: 4, startLineNumber: 3, startColumn: 13, endLineNumber: 3, endColumn: 20, message: "Property 'missing' not found in Root" },
            { severity: 4, startLineNumber: 4, startColumn: 13, endLineNumber: 4, endColumn: 20, message: "Property 'missing' not found in Root" },
            { severity: 4, startLineNumber: 5, startColumn: 9, endLineNumber: 5, endColumn: 16, message: "Property 'missing' not found in Root" },
            { severity: 4, startLineNumber: 6, startColumn: 7, endLineNumber: 6, endColumn: 14, message: "Property 'missing' not found in Root" },
            { severity: 4, startLineNumber: 7, startColumn: 12, endLineNumber: 7, endColumn: 19, message: "Property 'missing' not found in Root" },
        ]);
    });
});
