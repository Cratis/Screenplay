// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardApp } from '../BoardApp';
import { prepareBoardDom } from '../../Views/for_DependencyMapView/given/real_board_dom';
import { assertReadability, checkedChoices, choose, readabilityModel } from './given/readability_board';

vi.mock('../useHost', () => ({ useHost: () => ({ model: readabilityModel, refresh: vi.fn(), toggleFullscreen: vi.fn() }) }));

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

describe('when choosing readability on the real MCP board', () => {
    it('should start comfortable and medium with the existing native View defaults', async () => {
        assertReadability(host, 'comfortable', '1');
        (await checkedChoices(host)).should.deep.equal(['Comfortable', 'Medium']);
        (await checkedChoices(host, 'View')).should.deep.equal(['Full', 'Arrows']);
    });

    it('should offer every spacing and text choice by keyboard without replacing the canvas or grid', async () => {
        const canvas = host.querySelector('.canvas-surface');
        const grid = host.querySelector('.event-modeling-collection-grid');
        (canvas !== null).should.be.true;
        (grid !== null).should.be.true;
        for (const [label, density] of [['Compact', 'compact'], ['Comfortable', 'comfortable'], ['Spacious', 'spacious']]) {
            await choose(host, label);
            assertReadability(host, density, '1');
            (await checkedChoices(host)).should.deep.equal([label, 'Medium']);
        }
        for (const [label, scale] of [['Small', '0.9'], ['Medium', '1'], ['Large', '1.25']]) {
            await choose(host, label);
            assertReadability(host, 'spacious', scale);
            (await checkedChoices(host)).should.deep.equal(['Spacious', label]);
        }
        await choose(host, 'Compact');
        assertReadability(host, 'compact', '1.25');
        (host.querySelector('.canvas-surface') === canvas).should.be.true;
        (host.querySelector('.event-modeling-collection-grid') === grid).should.be.true;
    });

    it('should preserve native View and readability choices in both directions', async () => {
        await choose(host, 'Spacious');
        await choose(host, 'Large');
        for (const label of ['Properties', 'Overview', 'Lines']) await choose(host, label, 'View');
        assertReadability(host, 'spacious', '1.25');
        (await checkedChoices(host, 'View')).should.deep.equal(['Overview', 'Properties', 'Lines']);
        await choose(host, 'Compact');
        await choose(host, 'Small');
        (await checkedChoices(host, 'View')).should.deep.equal(['Overview', 'Properties', 'Lines']);
        for (const label of ['Full', 'Arrows']) await choose(host, label, 'View');
        assertReadability(host, 'compact', '0.9');
        (await checkedChoices(host, 'View')).should.deep.equal(['Full', 'Properties', 'Arrows']);
    });
});
