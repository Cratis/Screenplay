// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating a projection header and a draft matches a saved schema', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        const validator = aProjectionValidator();
        validator.setDraftReadModel({ identifier: 'Root', displayName: 'RootView', containerName: 'Models', schema: { name: 'Draft', properties: { draftOnly: { type: 'string' } } } });
        markers = validator.validate(modelFor('  projection App.Orders => RootView\n  draftOnly = value\n  rootOnly = value'));
    });

    it('should prefer the draft schema and report its informational marker first', () => {
        markers.should.deep.equal([
            { severity: 2, startLineNumber: 1, startColumn: 26, endLineNumber: 1, endColumn: 34, message: "Read model 'RootView' is a draft (not yet saved)" },
            { severity: 4, startLineNumber: 3, startColumn: 3, endLineNumber: 3, endColumn: 11, message: "Property 'rootOnly' not found in Draft" },
        ]);
    });
});
