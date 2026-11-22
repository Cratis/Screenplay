// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when leaving nested projection contexts and siblings have equal indentation', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  children children identified by id\n  rootOnly = value\n  from Known\n  from Known'));
    });

    it('should retain the child schema and its event map without a dedent', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 3, startColumn: 3, endLineNumber: 3, endColumn: 11, message: "Property 'rootOnly' not found in Child" },
            { severity: 8, startLineNumber: 5, startColumn: 8, endLineNumber: 5, endColumn: 13, message: "Duplicate event type 'Known' - event types can only be used once at each level" },
        ]);
    });
});
