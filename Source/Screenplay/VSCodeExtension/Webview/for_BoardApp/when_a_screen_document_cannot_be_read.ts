// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { a_board, a_model_with_a_screen, a_shown_board, type ShownBoard } from './given/a_board_with_screens';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

let board: ShownBoard;

// The same board, with the screen's document replaced by text no reader can make a screen of - what a host
// sees when a message was truncated or a document was written by something older than the reader.
const unreadable = () => {
    const message = a_board(a_model_with_a_screen);
    const screens = Object.fromEntries(Object.keys(message.screens).map(key => [key, 'not a scene document']));
    return { ...message, screens };
};

beforeEach(async () => {
    board = await a_shown_board(unreadable());
});
afterEach(async () => {
    await board.close();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when a screen document cannot be read', () => {
    it('should draw the board\'s own placeholder window', () => {
        (board.host.querySelector('.event-modeling-grid-ui-placeholder') !== null).should.be.true;
    });

    it('should draw no screen', () => {
        (board.host.querySelector('[data-screen-thumbnail-body]') === null).should.be.true;
    });

    it('should keep the board itself drawn', () => {
        (board.host.querySelector('.screenplay-board__failure') === null).should.be.true;
    });
});
