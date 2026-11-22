// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating schema contexts and a variant uses a display name', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  variant VariantView\n    variantOnly = value\n    rootOnly = value\n    key Composite\n    join missing on absent\n    join children on absent'));
    });

    it('should use the variant for assignments but the root for independent join and key checks', () => {
        markers.should.deep.equal([
            { severity: 4, startLineNumber: 4, startColumn: 5, endLineNumber: 4, endColumn: 13, message: "Property 'rootOnly' not found in Variant" },
            { severity: 4, startLineNumber: 6, startColumn: 10, endLineNumber: 6, endColumn: 17, message: "Collection property 'missing' not found in read model 'Root'" },
            { severity: 4, startLineNumber: 6, startColumn: 21, endLineNumber: 6, endColumn: 27, message: "Property 'absent' not found in read model 'Root'" },
            { severity: 4, startLineNumber: 7, startColumn: 22, endLineNumber: 7, endColumn: 28, message: "Property 'absent' not found in read model 'Root'" },
        ]);
    });
});
