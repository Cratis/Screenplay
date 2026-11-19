// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardApp } from '../BoardApp';
import { prepareBoardDom } from '../../Views/for_DependencyMapView/given/real_board_dom';
import { screenSource, screenInAFileSource } from './given/a_board_with_screens';

const host_showing = vi.hoisted(() => ({ source: '', dark: true }));

vi.mock('../useHost', () => ({ useHost: () => ({
    model: { application: 'Orders', documents: [{ path: 'orders.play', source: host_showing.source }] },
    dark: host_showing.dark,
    refresh: vi.fn(),
    toggleFullscreen: vi.fn(),
}) }));

let host: HTMLDivElement;
let root: Root;

async function board(source: string, dark = true) {
    host_showing.source = source;
    host_showing.dark = dark;
    prepareBoardDom();
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
    await act(async () => root.render(createElement(BoardApp)));
}

afterEach(async () => {
    await act(async () => root.unmount());
    host.remove();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when a slice states a screen on the MCP board', () => {
    beforeEach(() => board(screenSource));

    it('should draw the screen itself', () => (host.querySelector('.screen-thumbnail') !== null).should.be.true);
    it('should draw what the screen says', () => host.querySelector('.screen-thumbnail')!.textContent!.should.contain('Orders'));
    it('should not draw the placeholder window in its place', () =>
        (host.querySelector('.event-modeling-grid-ui-preview .event-modeling-grid-ui-placeholder') === null).should.be.true);
    it('should say nothing was left undrawn', () => (host.querySelector('.screenplay-mcp-board__notes') === null).should.be.true);
});

describe('when a screen cannot be drawn on the MCP board', () => {
    beforeEach(() => board(screenInAFileSource));

    it('should draw the placeholder window', () =>
        (host.querySelector('.event-modeling-grid-ui-preview .event-modeling-grid-ui-placeholder') !== null).should.be.true);
    it('should draw no screen', () => (host.querySelector('.screen-thumbnail') === null).should.be.true);
    it('should say why', () => host.querySelector('.screenplay-mcp-board__notes')!.textContent!.should.contain('screen note'));
    it('should not count it as an error', () => (host.querySelector('.screenplay-mcp-board__problems') === null).should.be.true);
});

describe('when the host shows the board on a light surface', () => {
    beforeEach(() => board(screenSource, false));

    it('should still draw the screen', () => (host.querySelector('.screen-thumbnail') !== null).should.be.true);
});
