// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { compileEventModelApplication, dependencyMapFor, toEventModelDocument } from '@cratis/screenplay-event-models';
import { BoardApp } from '../BoardApp';
import { prepareBoardDom } from '../../../Views/for_DependencyMapView/given/real_board_dom';
import { expectRoutesOnlyInTheBadge, routedSpecificationSource } from '../../../Views/for_DependencyMapView/given/routed_specifications';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

let host: HTMLDivElement;
let root: Root;
const application = compileEventModelApplication([{ path: 'work.play', source: routedSpecificationSource }]).value;

beforeEach(async () => {
    prepareBoardDom();
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
    await act(async () => root.render(createElement(BoardApp)));
    await act(async () => window.dispatchEvent(new MessageEvent('message', { data: {
        type: 'show', document: toEventModelDocument(application, 'Work'), dependencies: dependencyMapFor(application), problems: [],
    } })));
});
afterEach(async () => {
    await act(async () => root.unmount());
    host.remove();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when showing specification routes in the real VS Code board', () => {
    it('should keep routing out of the header and in the information badge of the routed specification', () => {
        expectRoutesOnlyInTheBadge(host.querySelector('.screenplay-board-view')!);
    });
});
