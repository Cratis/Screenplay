// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating a projection header and the header uses a display name', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        const validator = aProjectionValidator();
        validator.setReadModels([
            { identifier: 'Domain.Root', displayName: 'RootView', schema: { name: 'First', properties: { rootOnly: { type: 'string' } } } },
            { identifier: 'RootView', displayName: 'Other', schema: { name: 'Second', properties: {} } },
        ]);
        markers = validator.validate(modelFor('projection App.Orders => RootView\n  rootOnly = value\n  missing = value'));
    });

    it('should resolve the first matching saved model', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 3, startColumn: 3, endLineNumber: 3, endColumn: 10, message: "Property 'missing' not found in First" },
        ]);
    });
});
