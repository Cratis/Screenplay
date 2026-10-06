// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

const source = `
domain Reporting
module Reporting
  feature Assessments
    slice StateChange AssessChange
      command AssessChange
        changeId Uuid identifier
        produces ChangeAssessed
          for changeId
      event ChangeAssessed
        changeId Uuid
      specification Denied
        given caller
          claim "__proto__" = "one"
          claim "toString" = "two"
          claim "toString" = "three"
          claim "constructor" = "four"
        when AssessChange
          changeId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        then denied
`;

describe('when mapping caller claims named like object members', () => {
    let claims: Record<string, string>;

    beforeAll(() => {
        const slice = slice_named(toEventModelDocument(parse(source).value, 'Reporting'), 'AssessChange');
        claims = (slice.specifications[0] as SliceSpecificationDocument).caller!.claims;
    });

    it('should keep the claim named __proto__', () => {
        Object.getOwnPropertyDescriptor(claims, '__proto__')!.value.should.equal('one');
    });

    it('should join repeated claims of the same type', () => {
        Object.getOwnPropertyDescriptor(claims, 'toString')!.value.should.equal('two, three');
    });

    it('should keep a claim named like an inherited member', () => {
        Object.getOwnPropertyDescriptor(claims, 'constructor')!.value.should.equal('four');
    });
});
