// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const application = parse('identity\n  department String from claim "department"\nmodule M\n  feature F\n    slice StateChange S\n      command Record\n        value String\n        produces Recorded\n          value = value\n      event Recorded\n        value String').value;

describe('when mapping identity metadata', () => {
    it('should leave the business event board unchanged', () => {
        toEventModelDocument(application, 'Example').should.deep.equal(toEventModelDocument({ ...application, identity: null }, 'Example'));
    });
});
