// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { compileEventModelApplication, dependencyMapFor, toEventModelDocument } from '@cratis/screenplay-event-models';
import { BoardApp } from '../BoardApp';
import { activateWithKeyboard, prepareBoardDom, toolbarButton } from '../../../Views/for_DependencyMapView/given/real_board_dom';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

let host: HTMLDivElement;
let root: Root;
const application = compileEventModelApplication([{ path: 'work.play', source: 'module Work\n  feature Record\n    slice StateChange RecordWork\n      event WorkRecorded' }]).value;

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

describe('when switching the real VS Code board with the keyboard', () => {
    it('should dismiss its body-portaled View menu without replacing the canvas or grid', async () => {
        const board = host.querySelector('.screenplay-board-view')!;
        const canvas = board.querySelector('.canvas-surface');
        const grid = board.querySelector('.event-modeling-collection-grid');
        expect(canvas).not.toBeNull();
        expect(grid).not.toBeNull();
        await activateWithKeyboard(toolbarButton(board, 'View'));
        const menu = document.body.querySelector('.event-modeling-menu-dropdown-menu')!;
        expect(menu).not.toBeNull();
        expect(board.contains(menu)).toBe(false);
        expect(menu.querySelectorAll('button').length).toBeGreaterThan(0);
        await activateWithKeyboard(toolbarButton(board, 'Module and feature dependencies'));
        expect(document.body.querySelector('.event-modeling-menu-dropdown-menu')).toBeNull();
        expect(board.getAttribute('aria-hidden')).toBe('true');
        expect(board.hasAttribute('inert')).toBe(true);
        expect(board.querySelector('.canvas-surface')).toBe(canvas);
        expect(board.querySelector('.event-modeling-collection-grid')).toBe(grid);
        await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-dependency-map-host')!, 'Event model board'));
        expect(board.querySelector('.canvas-surface')).toBe(canvas);
        expect(board.querySelector('.event-modeling-collection-grid')).toBe(grid);
        expect(document.body.querySelector('.event-modeling-menu-dropdown-menu')).toBeNull();
    });
    it('should focus the matching toolbar button in the newly visible view in both directions', async () => {
        const board = host.querySelector('.screenplay-board-view')!;
        const map = host.querySelector('.screenplay-dependency-map-host')!;
        await activateWithKeyboard(toolbarButton(board, 'Module and feature dependencies'));
        expect(document.activeElement).toBe(toolbarButton(map, 'Module and feature dependencies'));
        await activateWithKeyboard(toolbarButton(map, 'Event model board'));
        expect(document.activeElement).toBe(toolbarButton(board, 'Event model board'));
    });
});
