// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when leaving nested projection contexts and a join is followed by a parent assignment', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders => Root\n  join children on id\n    events Known\nmissing = value'));
    });

    it('should omit the warning after dedenting pops a schema the join never pushed', () => {
        markers.should.deep.equal([]);
    });
});
