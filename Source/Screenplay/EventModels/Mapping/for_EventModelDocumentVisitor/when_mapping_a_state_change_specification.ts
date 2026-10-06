// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument, SliceDocument } from '../../Document/EventModelDocument';
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
      specification RecordsTheDecision
        given caller
          authenticated
        when AssessChange
          changeId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          impact = "reassess"
        then ChangeAssessed
          impact = "reassess"
`;

describe('when mapping a state change specification', () => {
    let document: EventModelDocument;
    let slice: SliceDocument;

    beforeAll(() => {
        document = toEventModelDocument(parse(source).value, 'Reporting');
        slice = slice_named(document, 'AssessChange');
    });

    it('should carry the name of the command under when so the board draws it once', () => {
        [slice.specifications[0].when!.name, slice.command!.name].should.deep.equal(['AssessChange', 'AssessChange']);
    });

    it('should carry the values and the command it sets off', () => {
        [slice.specifications[0].when!.values, slice.specifications[0].when!.commandId].should.deep.equal([
            { changeId: '3fa85f64-5717-4562-b3fc-2c963f66afa6', impact: 'reassess' },
            slice.command!.id,
        ]);
    });

    it('should carry the caller', () => {
        slice.specifications[0].caller!.should.deep.equal({ authenticated: true, roles: [], claims: {} });
    });

    it('should list each given and then step once', () => {
        [slice.specifications[0].given.length, slice.specifications[0].thenEvents.length].should.deep.equal([0, 1]);
    });
});
