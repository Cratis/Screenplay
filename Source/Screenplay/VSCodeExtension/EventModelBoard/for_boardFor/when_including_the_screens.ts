// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { screenDocumentFor, type EventModelDocument } from '@cratis/screenplay-event-models';
import { boardFor } from '../compileBoard';

const withAScreen = parse([
    'module Orders',
    '  feature Browsing',
    '    slice StateView Orders',
    '      readmodel Order',
    '        name string',
    '      screen Orders',
    '        title "Orders"',
].join('\n'), 'work.play');

const withoutAScreen = parse([
    'module Orders',
    '  feature Recording',
    '    slice StateChange RecordOrder',
    '      event OrderRecorded',
].join('\n'), 'work.play');

describe('when including the screens', () => {
    const board = boardFor(withAScreen, 'Orders');
    const slice = (board.document as EventModelDocument).collections[0].modules[0].features[0].slices[0];

    it('should carry the screen of the cell the board draws', () =>
        (screenDocumentFor(board.screens, slice.id, slice.actors[0].id) !== undefined).should.be.true);

    it('should still say the role has a screen', () => slice.actors[0].hasScreen.should.be.true);

    it('should report nothing it could not draw', () => board.screenFindings.should.deep.equal([]));
});

describe('when the board has no screen', () => {
    const board = boardFor(withoutAScreen, 'Orders');

    it('should carry no screen', () => Object.keys(board.screens).length.should.equal(0));
    it('should report nothing', () => board.screenFindings.should.deep.equal([]));
});

describe('when a screen cannot be drawn', () => {
    const board = boardFor(parse([
        'module Orders',
        '  feature Browsing',
        '    slice StateView Orders',
        '      readmodel Order',
        '        name string',
        '      screen Orders',
        '        file "orders.screen.json"',
    ].join('\n'), 'work.play'), 'Orders');
    const slice = (board.document as EventModelDocument).collections[0].modules[0].features[0].slices[0];

    it('should carry no screen for it', () =>
        (screenDocumentFor(board.screens, slice.id, slice.actors[0].id) === undefined).should.be.true);

    it('should say so rather than fail', () => board.screenFindings.map(finding => finding.screen).should.deep.equal(['Orders']));

    it('should leave the board drawable', () => board.problems.should.deep.equal([]));
});
