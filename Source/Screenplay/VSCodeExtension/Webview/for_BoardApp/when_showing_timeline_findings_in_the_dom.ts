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
import { expectTimelineFindingOnTheConsumer, timelineFindingSource } from '../../../Views/for_DependencyMapView/given/timeline_findings';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

let host: HTMLDivElement;
let root: Root;

beforeEach(async () => {
    prepareBoardDom();
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
    await act(async () => root.render(createElement(BoardApp)));
    await act(async () => window.dispatchEvent(new MessageEvent('message', { data: boardFor(parse(timelineFindingSource, 'work.play'), 'Work') })));
});
afterEach(async () => {
    await act(async () => root.unmount());
    host.remove();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when showing timeline findings in the real VS Code board', () => {
    it('should mark only the consumer header alongside the unchanged State View event warning', () => {
        expectTimelineFindingOnTheConsumer(host.querySelector('.screenplay-board-view')!);
    });
});
