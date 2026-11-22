// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating a projection header and the header is invalid', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('# comment\n  projection Bad-Name\n  from Missing\n    value = $eventContext.'));
    });

    it('should report only the trimmed header range and stop further checks', () => {
        markers.should.deep.equal([
            { severity: 8, startLineNumber: 2, startColumn: 1, endLineNumber: 2, endColumn: 20, message: 'Projection definition must start with "projection <ProjectionName>" or "projection <ProjectionName> => <ReadModelIdentifier>"' },
        ]);
    });
});
