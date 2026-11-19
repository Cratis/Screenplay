// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { screenDocumentFor } from '@cratis/screenplay-event-models';
import { compileBoards } from '../compileBoards';

const source = `module Orders
  feature Browsing
    slice StateView Orders
      readmodel Order
        name string
      screen Orders
        title "Orders"
`;

const withoutScreens = `module Orders
  feature Recording
    slice StateChange RecordOrder
      event OrderRecorded
`;

const sliceOf = (board: ReturnType<typeof compileBoards>['current']) =>
    board.document.collections[0].modules[0].features[0].slices[0];

describe('when compiling the screens for current and proposed boards', () => {
    const boards = compileBoards({
        application: 'Orders',
        documents: [{ path: 'orders.play', source }],
        changes: [{ path: 'orders.play', source: source.replace('"Orders"', '"All orders"') }],
    });

    it('should carry the screen of the board as it is', () => {
        const slice = sliceOf(boards.current);
        screenDocumentFor(boards.current.screens, slice.id, slice.actors[0].id)!.should.contain('Orders');
    });

    it('should follow the proposal in its own screens', () => {
        const slice = sliceOf(boards.proposed!);
        screenDocumentFor(boards.proposed!.screens, slice.id, slice.actors[0].id)!.should.contain('All orders');
    });

    it('should report nothing it could not draw', () => {
        boards.current.screenFindings.should.deep.equal([]);
        boards.proposed!.screenFindings.should.deep.equal([]);
    });
});

describe('when compiling a board with no screen', () => {
    const boards = compileBoards({ application: 'Orders', documents: [{ path: 'orders.play', source: withoutScreens }] });

    it('should carry no screen', () => Object.keys(boards.current.screens).length.should.equal(0));
});
