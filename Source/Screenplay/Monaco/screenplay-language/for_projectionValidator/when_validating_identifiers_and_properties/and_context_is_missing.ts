// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { aProjectionValidator, modelFor } from '../given/a_projection_validator';

describe('when validating identifiers and properties and context is missing', () => {
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        markers = aProjectionValidator().validate(modelFor('projection Orders\n  key Missing\n  identified by absent\n  missing = value\n  increment missing\n  decrement missing\n  count missing\n  add missing by 1\n  subtract missing by 1'));
    });

    it('should skip schema-dependent identifier and property warnings', () => {
        markers.should.deep.equal([]);
    });
});
