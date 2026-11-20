// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { afterEach, beforeEach, describe, it, vi } from 'vitest';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

// A renderer that fails on the screen it is given - a blueprint the bundle does not carry, a document shaped
// in a way the renderer did not expect. The webview must survive it: a board that empties itself is the
// failure this guards against.
vi.mock('@cratis/scene.editor', async importOriginal => ({
    ...(await importOriginal<typeof import('@cratis/scene.editor')>()),
    ScreenThumbnail: () => { throw new Error('the screen could not be drawn'); },
}));

let board: Awaited<ReturnType<typeof import('./given/a_board_with_screens')['a_shown_board']>>;

beforeEach(async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const given = await import('./given/a_board_with_screens');
    board = await given.a_shown_board(given.a_board(given.a_model_with_a_screen));
});
afterEach(async () => {
    await board.close();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when drawing a screen throws', () => {
    it('should say the board failed to draw rather than showing nothing', () => {
        board.host.querySelector('.screenplay-board__failure')!.textContent!.should.contain('the screen could not be drawn');
    });

    it('should offer a way back to a board that draws', () => {
        (board.host.querySelector('.screenplay-board__failure button') !== null).should.be.true;
    });

    it('should keep the problems area of the webview', () => {
        (board.host.querySelector('.screenplay-board') !== null).should.be.true;
    });
});
