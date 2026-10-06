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
        impact String
        produces ChangeAssessed
          for changeId
          impact = impact
      event ChangeAssessed
        impact String
      specification Denied
        given caller
          authenticated
          role "Reviewer"
          role "Auditor"
          claim "department" = "risk"
          claim "department" = "audit"
        when AssessChange
          changeId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          impact = $context.occurred
        then denied
      specification Refused
        when AssessChange
          changeId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          impact = ""
        then error "Impact is required"
        then error
`;

describe('when mapping what a specification states', () => {
    let denied: SliceSpecificationDocument;
    let refused: SliceSpecificationDocument;

    beforeAll(() => {
        const slice = slice_named(toEventModelDocument(parse(source).value, 'Reporting'), 'AssessChange');
        denied = slice.specifications[0];
        refused = slice.specifications[1];
    });

    it('should carry the caller with its roles and claims', () => {
        denied.caller!.should.deep.equal({ authenticated: true, roles: ['Reviewer', 'Auditor'], claims: { department: 'risk, audit' } });
    });

    it('should leave out the caller when the specification states none', () => {
        (refused.caller === undefined).should.be.true;
    });

    it('should carry a value that is not a literal as written', () => {
        (denied.when!.values.impact as string).should.equal('$context.occurred');
    });

    it('should carry a denial', () => {
        denied.thenErrors.map(error => error.name).should.deep.equal(['denied']);
    });

    it('should carry the message of an error and keep an error without one', () => {
        refused.thenErrors.map(error => error.message).should.deep.equal(['Impact is required', undefined]);
    });
});
