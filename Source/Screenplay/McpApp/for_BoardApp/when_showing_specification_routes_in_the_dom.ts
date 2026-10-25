// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardApp } from '../BoardApp';
import { prepareBoardDom } from '../../Views/for_DependencyMapView/given/real_board_dom';
import { expectRoutesOnlyInTheBadge } from '../../Views/for_DependencyMapView/given/routed_specifications';

vi.mock('../useHost', async () => {
    const { routedSpecificationSource } = await import('../../Views/for_DependencyMapView/given/routed_specifications');
    return { useHost: () => ({
        model: { application: 'Work', documents: [{ path: 'work.play', source: routedSpecificationSource }] },
        refresh: vi.fn(), toggleFullscreen: vi.fn(),
    }) };
});

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

describe('when showing specification routes in the real MCP board', () => {
    it('should keep routing out of the header and in the information badge of the routed specification', () => {
        expectRoutesOnlyInTheBadge(host.querySelector('.screenplay-board-view')!);
    });
});
