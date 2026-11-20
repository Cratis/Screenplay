// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardApp } from '../BoardApp';
import { prepareBoardDom } from '../../Views/for_DependencyMapView/given/real_board_dom';
import { modelWithAScreen } from './given/a_model_with_screens';

vi.mock('../useHost', () => ({ useHost: () => ({ model: modelWithAScreen, refresh: vi.fn(), toggleFullscreen: vi.fn() }) }));

let host: HTMLDivElement;
let root: Root;

beforeEach(async () => {
    prepareBoardDom();
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
    await act(async () => root.render(createElement(BoardApp)));
});
afterEach(async () => {
    await act(async () => root.unmount());
    host.remove();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when a slice screen has a document on the MCP board', () => {
    it('should draw the screen rather than the placeholder window', () => {
        (host.querySelector('[data-screen-thumbnail-body]') !== null).should.be.true;
    });

    it('should draw what the screen says', () => {
        host.querySelector('[data-screen-thumbnail-body]')!.textContent!.should.contain('Orders');
    });

    it('should leave the board\'s own placeholder window undrawn', () => {
        (host.querySelector('.event-modeling-grid-ui-placeholder') === null).should.be.true;
    });

    it('should draw the screen where it cannot be interacted with', () => {
        host.querySelector('[data-screen-thumbnail-body]')!.hasAttribute('inert').should.be.true;
    });
});
