// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

// Phase 1 changes authoring metadata, not board execution/lineage or event payload shape.
describe('when mapping report-only subjects', () => {
    it('should leave the board and payload schema unchanged', () => {
        const source = 'module M\n  feature F\n    slice StateChange S\n      command C\n        id Uuid identifier\n        customerId Uuid\n        produces event E\n          customerId Uuid MARK = customerId';
        const marked = parse(source.replace('MARK', 'subject'));
        const unmarked = parse(source.replace('MARK', ''));
        marked.success.should.be.true;
        toEventModelDocument(marked.value, 'Subjects').should.deep.equal(toEventModelDocument(unmarked.value, 'Subjects'));
    });
});
