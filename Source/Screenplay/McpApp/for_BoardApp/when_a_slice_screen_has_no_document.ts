// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardApp } from '../BoardApp';
import { prepareBoardDom } from '../../Views/for_DependencyMapView/given/real_board_dom';
import { modelWithAnUndrawableScreen } from './given/a_model_with_screens';

vi.mock('../useHost', () => ({ useHost: () => ({ model: modelWithAnUndrawableScreen, refresh: vi.fn(), toggleFullscreen: vi.fn() }) }));

let host: HTMLDivElement;
let root: Root;
let errors: unknown[][];

beforeEach(async () => {
    prepareBoardDom();
    errors = [];
    vi.spyOn(console, 'error').mockImplementation((...args: unknown[]) => void errors.push(args));
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

describe('when a slice screen has no document on the MCP board', () => {
    it('should draw the board\'s own placeholder window', () => {
        (host.querySelector('.event-modeling-grid-ui-placeholder') !== null).should.be.true;
    });

    it('should draw no screen', () => {
        (host.querySelector('[data-screen-thumbnail-body]') === null).should.be.true;
    });

    it('should report nothing as an error', () => errors.should.deep.equal([]));
});
