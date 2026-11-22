// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when leaving nested projection contexts and a dedent is partial', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  children children identified by id\n      childOnly = value\n    rootOnly = value\n  rootOnly = value'));
    });

    it('should retain the observed child schema even after its indent entry is removed', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 4, startColumn: 5, endLineNumber: 4, endColumn: 13, message: "Property 'rootOnly' not found in Child" },
            { severity: 4, startLineNumber: 5, startColumn: 3, endLineNumber: 5, endColumn: 11, message: "Property 'rootOnly' not found in Child" },
        ]);
    });
});
