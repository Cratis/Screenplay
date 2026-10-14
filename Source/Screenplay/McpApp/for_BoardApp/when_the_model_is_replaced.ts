// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { createElement } from 'react';
import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardApp } from '../BoardApp';

const board = vi.hoisted(() => ({ scrollTo: undefined as unknown as ReturnType<typeof vi.fn>, model: undefined as unknown, frames: [] as (() => void)[], cancelled: [] as number[] }));

vi.mock('../useHost', () => ({ useHost: () => ({ model: board.model, refresh: vi.fn(), toggleFullscreen: vi.fn() }) }));
vi.mock('@cratis/event-models', async importOriginal => {
    const original = await importOriginal<typeof import('@cratis/event-models')>();
    return {
        ...original,
        readEventModelDocument: (document: unknown) => document,
        EventModelBoard: ({ handle }: { handle: { current: unknown } }) => {
            handle.current = { scrollTo: board.scrollTo };
            return createElement('div', { className: 'fake-board' });
        },
    };
});

const play = (field: string) => `module Work
  feature Record
    slice StateChange RecordWork
      command RecordWork
        ${field} String
        produces WorkRecorded
          ${field} = ${field}
      event WorkRecorded
        ${field} String
`;
const toolResult = (proposed: string) => ({ application: 'Work', documents: [{ path: 'work.play', source: play('name') }], changes: [{ path: 'work.play', source: proposed }] });

let host: HTMLDivElement;
let root: Root;
let mounted = true;

const render = () => act(async () => root.render(createElement(BoardApp)));
const unmount = async () => { if (mounted) { mounted = false; await act(async () => root.unmount()); } };
const runFrames = () => act(async () => { board.frames.splice(0).forEach(frame => frame()); });

beforeEach(async () => {
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true);
    board.scrollTo = vi.fn().mockResolvedValue(true);
    board.frames = [];
    board.cancelled = [];
    vi.stubGlobal('requestAnimationFrame', (frame: () => void) => board.frames.push(frame));
    vi.stubGlobal('cancelAnimationFrame', (id: number) => { board.cancelled.push(id); board.frames[id - 1] = () => undefined; });
    board.model = toolResult(play('title'));
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
    mounted = true;
    await render();
});
afterEach(async () => {
    await unmount();
    host.remove();
    vi.unstubAllGlobals();
});

describe('when the first board has been drawn', () => {
    it('should leave opening at the first change to the board', async () => {
        await runFrames();
        board.scrollTo.mock.calls.length.should.equal(0);
    });
});

describe('when the tool result is replaced while the same view is shown', () => {
    beforeEach(async () => {
        board.model = toolResult(play('label'));
        await render();
        await runFrames();
    });

    it('should scroll to the first change of the new document', () => board.scrollTo.mock.calls.length.should.equal(1));
    it('should animate', () => board.scrollTo.mock.calls[0][1].should.deep.equal({ animate: true }));
});

describe('when the tool result is replaced again before the scheduled frame', () => {
    beforeEach(async () => {
        board.model = toolResult(play('label'));
        await render();
        board.model = toolResult(play('summary'));
        await render();
        await runFrames();
    });

    it('should cancel the first frame', () => board.cancelled.should.have.lengthOf(1));
    it('should scroll once, for the newer document', () => board.scrollTo.mock.calls.length.should.equal(1));
});

describe('when the app unmounts before the scheduled frame', () => {
    beforeEach(async () => {
        board.model = toolResult(play('label'));
        await render();
        await unmount();
        await runFrames();
    });

    it('should cancel the frame', () => board.cancelled.should.have.lengthOf(1));
    it('should not scroll', () => board.scrollTo.mock.calls.length.should.equal(0));
});
