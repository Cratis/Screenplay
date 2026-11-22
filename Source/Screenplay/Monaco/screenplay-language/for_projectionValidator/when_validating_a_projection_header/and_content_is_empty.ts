// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating a projection header and content is empty', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor(' \t\r\n'));
    });

    it('should return no markers', () => {
        markers.should.deep.equal([]);
    });
});
