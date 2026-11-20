// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { a_board, a_model_with_an_undrawable_screen, a_shown_board, type ShownBoard } from './given/a_board_with_screens';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

let board: ShownBoard;
let errors: unknown[][];

beforeEach(async () => {
    errors = [];
    vi.spyOn(console, 'error').mockImplementation((...args: unknown[]) => void errors.push(args));
    board = await a_shown_board(a_board(a_model_with_an_undrawable_screen));
});
afterEach(async () => {
    await board.close();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when a slice screen has no document', () => {
    it('should draw the board\'s own placeholder window', () => {
        (board.host.querySelector('.event-modeling-grid-ui-placeholder') !== null).should.be.true;
    });

    it('should draw no screen', () => {
        (board.host.querySelector('[data-screen-thumbnail-body]') === null).should.be.true;
    });

    it('should report nothing as an error', () => errors.should.deep.equal([]));

    it('should say which screen is drawn as a placeholder', () => {
        board.host.querySelector('.screenplay-board__screen-findings')!.textContent!.should.contain('Listing');
    });

    it('should not count it among the errors in the model', () => {
        board.host.querySelector('.screenplay-board__screen-findings')!.textContent!.should.not.contain('error');
    });
});
