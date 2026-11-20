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

// A renderer that fails on the screen it is given. The view must survive it: a board that empties itself is
// the failure this guards against.
vi.mock('@cratis/scene.editor', async importOriginal => ({
    ...(await importOriginal<typeof import('@cratis/scene.editor')>()),
    ScreenThumbnail: () => { throw new Error('the screen could not be drawn'); },
}));

let host: HTMLDivElement;
let root: Root;

beforeEach(async () => {
    prepareBoardDom();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
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

describe('when drawing a screen throws on the MCP board', () => {
    it('should say the board could not be drawn rather than showing nothing', () => {
        host.textContent!.should.contain('the screen could not be drawn');
    });

    it('should keep the view itself', () => {
        (host.querySelector('.screenplay-mcp-board') !== null).should.be.true;
    });
});
