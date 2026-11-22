// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when leaving nested projection contexts and events are reused in variant and children scopes', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  from Known\n  children children identified by id\n    from Known\n  variant Variant\n    enters on Known\n    from Known'));
    });

    it('should isolate event names in each new scope', () => {
        markers.should.deep.equal([
            { severity: 8, startLineNumber: 7, startColumn: 10, endLineNumber: 7, endColumn: 15, message: "Duplicate event type 'Known' - event types can only be used once at each level" },
        ]);
    });
});
