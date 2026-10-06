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

// The board draws a State Change slice's own command under When, and draws a specification's When on top of it
// whenever the step has a name - so a step that names the slice's command is a second card for the same command.
function names_the_board_draws_under_when(slice: SliceDocument): string[] {
    const specification = slice.specifications[0];
    return [slice.command?.name, specification.when?.name].filter((name): name is string => name !== undefined && name !== '');
}

describe('when mapping a state change specification', () => {
    let document: EventModelDocument;
    let slice: SliceDocument;

    beforeAll(() => {
        document = toEventModelDocument(parse(source).value, 'Reporting');
        slice = slice_named(document, 'AssessChange');
    });

    it('should show the command once under when', () => {
        names_the_board_draws_under_when(slice).should.deep.equal(['AssessChange']);
    });

    it('should still carry the literal values and the command it sets off', () => {
        [slice.specifications[0].when!.values, slice.specifications[0].when!.commandId].should.deep.equal([
            { changeId: '3fa85f64-5717-4562-b3fc-2c963f66afa6', impact: 'reassess' },
            slice.command!.id,
        ]);
    });

    it('should list each given and then step once', () => {
        [slice.specifications[0].given.length, slice.specifications[0].thenEvents.length].should.deep.equal([0, 1]);
    });
});
