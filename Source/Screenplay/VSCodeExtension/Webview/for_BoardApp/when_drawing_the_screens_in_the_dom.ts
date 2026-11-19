// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { BoardApp } from '../BoardApp';
import { boardFor } from '../../EventModelBoard/compileBoard';
import { prepareBoardDom } from '../../../Views/for_DependencyMapView/given/real_board_dom';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

const withAScreen = [
    'module Orders',
    '  feature Browsing',
    '    slice StateView Orders',
    '      readmodel Order',
    '        name string',
    '      screen Orders',
    '        title "Orders"',
].join('\n');

const screenInAFile = [
    'module Orders',
    '  feature Browsing',
    '    slice StateView Orders',
    '      readmodel Order',
    '        name string',
    '      screen Orders',
    '        file "orders.screen.json"',
].join('\n');

let host: HTMLDivElement;
let root: Root;

async function board(source: string) {
    prepareBoardDom();
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
    await act(async () => root.render(createElement(BoardApp)));
    await act(async () => window.dispatchEvent(new MessageEvent('message', { data: boardFor(parse(source, 'work.play'), 'Orders') })));
}

afterEach(async () => {
    await act(async () => root.unmount());
    host.remove();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when a slice states a screen', () => {
    beforeEach(() => board(withAScreen));

    it('should draw the screen itself', () => (host.querySelector('.screen-thumbnail') !== null).should.be.true);

    it('should draw what the screen says', () => host.querySelector('.screen-thumbnail')!.textContent!.should.contain('Orders'));

    it('should not draw the board\'s placeholder window in its place', () =>
        (host.querySelector('.event-modeling-grid-ui-preview .event-modeling-grid-ui-placeholder') === null).should.be.true);

    it('should say nothing was left undrawn', () => (host.querySelector('.screenplay-board__problems') === null).should.be.true);
});

describe('when a screen cannot be drawn', () => {
    beforeEach(() => board(screenInAFile));

    it('should draw the board\'s own placeholder window', () =>
        (host.querySelector('.event-modeling-grid-ui-preview .event-modeling-grid-ui-placeholder') !== null).should.be.true);

    it('should draw no screen', () => (host.querySelector('.screen-thumbnail') === null).should.be.true);

    it('should say why, without calling the model broken', () => {
        const notes = host.querySelector('.screenplay-board__problems')!;
        notes.className.should.contain('screenplay-board__problems--notes');
        notes.textContent!.should.contain('Orders');
    });

    it('should still draw the board', () => (host.querySelector('[data-board-density]') !== null).should.be.true);
});
