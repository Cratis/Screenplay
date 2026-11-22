// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating event references and the event catalog is empty', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        const validator = aProjectionValidator();
        validator.setEventSchemas([]);
        markers = validator.validate(modelFor('projection Orders\n  from Anything\n  enters on Anything\n  remove with Elsewhere'));
    });

    it('should allow unknown names while retaining duplicate checks', () => {
        markers.should.deep.equal([
            { severity: 8, startLineNumber: 3, startColumn: 13, endLineNumber: 3, endColumn: 21, message: "Duplicate event type 'Anything' - event types can only be used once at each level" },
        ]);
    });
});
