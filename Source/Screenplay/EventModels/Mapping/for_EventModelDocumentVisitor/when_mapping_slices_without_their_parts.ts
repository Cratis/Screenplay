// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';
import { slice_named } from './given/the_constructs_document';

describe('when mapping slices without their parts', () => {
    let document: EventModelDocument;

    beforeAll(() => {
        document = toEventModelDocument(parse([
            'module Customers',
            '  feature Onboarding',
            '    slice StateView Nothing',
            '    slice StateView Undeclared',
            '      query All => Customer[]',
            '    slice Automation Idle',
            '    slice StateChange Bare',
            '      command Bare',
            '      event Happened',
        ].join('\n')).value, 'Customers');
    });

    it('should still be a document the board can read', () => {
        problems_the_board_finds_in(document).should.deep.equal([]);
    });

    it('should show a state view without a read model', () => {
        (slice_named(document, 'Nothing').readModel === undefined).should.be.true;
    });

    it('should name a read model only a query returns, without a shape', () => {
        const readModel = slice_named(document, 'Undeclared').readModel!;
        [readModel.name, readModel.schema].should.deep.equal(['Customer', {}]);
    });

    it('should show an automation without a trigger', () => {
        (slice_named(document, 'Idle').automationTrigger === undefined).should.be.true;
    });

    it('should shape a command and an event without properties as empty', () => {
        [slice_named(document, 'Bare').command!.schema, slice_named(document, 'Bare').events[0].schema].should.deep.equal([{}, {}]);
    });
});

describe('when mapping an application without modules', () => {
    it('should draw no collection', () => {
        toEventModelDocument(parse('concept Id : Uuid').value, 'Empty').collections.should.deep.equal([]);
    });
});
