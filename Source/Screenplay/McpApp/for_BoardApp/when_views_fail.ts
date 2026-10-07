// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { createElement, type ReactNode } from 'react';
import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { BoardApp } from '../BoardApp';

const failures = vi.hoisted(() => ({ unreadable: false, boardDraw: false, mapDraw: false, compile: false }));

vi.mock('../useHost', () => ({ useHost: () => ({
    model: { application: 'Work', documents: [{ path: 'work.play', source: 'module Work\n  feature Record\n    slice StateChange RecordWork\n      event WorkRecorded' }] },
    refresh: vi.fn(), toggleFullscreen: vi.fn(),
}) }));
vi.mock('../compileBoards', async importOriginal => {
    const original = await importOriginal<typeof import('../compileBoards')>();
    return { ...original, compileBoards: (...args: Parameters<typeof original.compileBoards>) => {
        if (failures.compile) throw new Error('compilation failed');
        return original.compileBoards(...args);
    } };
});
vi.mock('@cratis/event-models', async importOriginal => {
    const original = await importOriginal<typeof import('@cratis/event-models')>();
    return {
        ...original,
        readEventModelDocument: (document: unknown) => {
            if (failures.unreadable) throw new Error('document unreadable');
            return document;
        },
        EventModelBoard: ({ toolbar }: { toolbar?: ReactNode }) => {
            if (failures.boardDraw) throw new Error('board exploded');
            return createElement('div', { className: 'fake-board' }, toolbar);
        },
    };
});
vi.mock('@cratis/screenplay-views', async importOriginal => {
    const original = await importOriginal<typeof import('@cratis/screenplay-views')>();
    return {
        ...original,
        DependencyMapView: (props: Parameters<typeof original.DependencyMapView>[0]) => {
            if (failures.mapDraw) throw new Error('map exploded');
            return createElement(original.DependencyMapView, props);
        },
    };
});

let host: HTMLDivElement;
let root: Root;

const render = async () => {
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
    await act(async () => root.render(createElement(BoardApp)));
};
const mapHost = () => host.querySelector('.screenplay-dependency-map-host')!;
const boardView = () => host.querySelector('.screenplay-board-view')!;
const toolbarButtons = (container: Element) => [...container.querySelectorAll('button')].map(button => button.getAttribute('aria-label'));

beforeEach(() => Object.assign(failures, { unreadable: false, boardDraw: false, mapDraw: false, compile: false }));
afterEach(async () => {
    await act(async () => root.unmount());
    host.remove();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when the board document cannot be read', () => {
    beforeEach(async () => { failures.unreadable = true; await render(); });

    it('should explain the failure in the board view', () => expect(boardView().textContent).toContain('document unreadable'));
    it('should keep the board and map controls in both views', () => {
        expect(toolbarButtons(boardView())).toContain('Module and feature dependencies');
        expect(toolbarButtons(mapHost())).toContain('Event model board');
    });
    it('should still draw the dependency map', () => expect(mapHost().querySelector('.screenplay-dependency-map')).not.toBeNull());
});

describe('when drawing the board fails', () => {
    beforeEach(async () => { failures.boardDraw = true; await render(); });

    it('should explain the failure in the board view with the controls kept', () => {
        expect(boardView().textContent).toContain('The board failed to draw: board exploded');
        expect(toolbarButtons(boardView())).toContain('Module and feature dependencies');
    });
    it('should keep the dependency map', () => {
        expect(mapHost().querySelector('.screenplay-dependency-map')).not.toBeNull();
        expect(toolbarButtons(mapHost())).toContain('Event model board');
    });
});

describe('when drawing the dependency map fails', () => {
    beforeEach(async () => { failures.mapDraw = true; await render(); });

    it('should explain the failure in the map view with the controls kept', () => {
        expect(mapHost().textContent).toContain('The dependency map failed to draw: map exploded');
        expect(toolbarButtons(mapHost())).toContain('Event model board');
    });
    it('should keep the board', () => expect(boardView().querySelector('.fake-board')).not.toBeNull());
});

describe('when compiling yields no map', () => {
    beforeEach(async () => { failures.compile = true; await render(); });

    it('should say the dependency map is unavailable', () => expect(mapHost().textContent).toContain('The dependency map is unavailable: compilation failed'));
    it('should explain the failure in the board view with the controls kept', () => {
        expect(boardView().textContent).toContain('The board could not be drawn: compilation failed');
        expect(toolbarButtons(boardView())).toContain('Module and feature dependencies');
    });
});
