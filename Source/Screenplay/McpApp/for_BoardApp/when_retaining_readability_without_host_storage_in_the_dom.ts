// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import type { VisualizedModel } from '../VisualizedModel';
import { BoardApp } from '../BoardApp';
import { activateWithKeyboard, prepareBoardDom, toolbarButton } from '../../Views/for_DependencyMapView/given/real_board_dom';
import { assertReadability, checkedChoices, choose, readabilityModel } from './given/readability_board';

const transport = vi.hoisted(() => ({
    capabilities: undefined as object | undefined,
    context: { displayMode: 'inline', availableDisplayModes: ['inline', 'fullscreen'] },
    ontoolresult: undefined as ((result: { structuredContent: VisualizedModel }) => void) | undefined,
    onhostcontextchanged: undefined as (() => void) | undefined,
    connect: vi.fn(async () => undefined),
    getHostContext: vi.fn(() => transport.context),
    getHostCapabilities: vi.fn(() => transport.capabilities),
    requestDisplayMode: vi.fn(async ({ mode }: { mode: string }) => ({ mode })),
    callServerTool: vi.fn(),
    updateModelContext: vi.fn(),
}));
vi.mock('@modelcontextprotocol/ext-apps/app-with-deps', () => ({ App: class { constructor() { return transport; } } }));

let host: HTMLDivElement;
let root: Root;

async function deliver(model = readabilityModel) {
    await act(async () => transport.ontoolresult!({ structuredContent: model }));
}

beforeEach(async () => {
    prepareBoardDom();
    vi.clearAllMocks();
    transport.context = { displayMode: 'inline', availableDisplayModes: ['inline', 'fullscreen'] };
    for (const storage of ['localStorage', 'sessionStorage'] as const) {
        vi.spyOn(window, storage, 'get').mockImplementation(() => { throw new DOMException('Storage denied', 'SecurityError'); });
    }
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
});
afterEach(async () => {
    await act(async () => root.unmount());
    host.remove();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

for (const capabilities of [undefined, {}]) {
    describe(`when retaining readability with ${capabilities === undefined ? 'absent' : 'empty'} host capabilities and denied browser storage`, () => {
        beforeEach(async () => {
            transport.capabilities = capabilities;
            await act(async () => root.render(createElement(BoardApp)));
            await deliver();
        });

        it('should retain session choices across redraws, model replacement, views and fullscreen without host saves', async () => {
            assertReadability(host, 'comfortable', '1');
            await choose(host, 'Spacious');
            await choose(host, 'Large');
            await act(async () => root.render(createElement(BoardApp)));
            assertReadability(host, 'spacious', '1.25');
            await deliver({ ...readabilityModel, documents: readabilityModel.changes!, changes: readabilityModel.documents });
            assertReadability(host, 'spacious', '1.25');
            await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-board-view')!, 'The application as it is'));
            assertReadability(host, 'spacious', '1.25');
            await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-board-view')!, 'The application as the change would leave it'));
            assertReadability(host, 'spacious', '1.25');
            await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-board-view')!, 'Module and feature dependencies'));
            const map = host.querySelector('.screenplay-dependency-map-host')!;
            (map.querySelector('button[aria-label="Spacing and text size"]') === null).should.be.true;
            await activateWithKeyboard(toolbarButton(map, 'Event model board'));
            assertReadability(host, 'spacious', '1.25');
            await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-board-view')!, 'Fullscreen'));
            transport.context = { ...transport.context, displayMode: 'fullscreen' };
            await act(async () => transport.onhostcontextchanged!());
            assertReadability(host, 'spacious', '1.25');
            (await checkedChoices(host)).should.deep.equal(['Spacious', 'Large']);
            transport.requestDisplayMode.mock.calls.should.deep.equal([[{ mode: 'fullscreen' }]]);
            transport.callServerTool.mock.calls.should.have.lengthOf(0);
            transport.updateModelContext.mock.calls.should.have.lengthOf(0);
        });

        it('should start a newly mounted app at defaults rather than promise restoration', async () => {
            await choose(host, 'Compact');
            await choose(host, 'Small');
            await act(async () => root.unmount());
            root = createRoot(host);
            await act(async () => root.render(createElement(BoardApp)));
            await deliver();
            assertReadability(host, 'comfortable', '1');
            (await checkedChoices(host)).should.deep.equal(['Comfortable', 'Medium']);
        });
    });
}
