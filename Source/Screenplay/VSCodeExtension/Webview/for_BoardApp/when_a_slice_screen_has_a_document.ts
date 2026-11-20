// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { a_board, a_model_with_a_screen, a_shown_board, type ShownBoard } from './given/a_board_with_screens';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

let board: ShownBoard;

beforeEach(async () => {
    board = await a_shown_board(a_board(a_model_with_a_screen));
});
afterEach(async () => {
    await board.close();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when a slice screen has a document', () => {
    it('should draw the screen rather than the placeholder window', () => {
        (board.host.querySelector('[data-screen-thumbnail-body]') !== null).should.be.true;
    });

    it('should draw what the screen says', () => {
        board.host.querySelector('[data-screen-thumbnail-body]')!.textContent!.should.contain('Orders');
    });

    it('should leave the board\'s own placeholder window undrawn', () => {
        (board.host.querySelector('.event-modeling-grid-ui-placeholder') === null).should.be.true;
    });

    it('should draw the screen where it cannot be interacted with', () => {
        board.host.querySelector('[data-screen-thumbnail-body]')!.hasAttribute('inert').should.be.true;
    });

    it('should say nothing about a screen it could not draw', () => {
        (board.host.querySelector('.screenplay-board__screen-findings') === null).should.be.true;
    });
});
