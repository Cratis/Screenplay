// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardApp } from '../BoardApp';
import { prepareBoardDom } from '../../Views/for_DependencyMapView/given/real_board_dom';
import { allSliceLinks, emphasisCount, emphasisOf, layOutSliceColumns, waitForAFrame, scopeHeader, slicelinks } from '../../Views/for_DependencyMapView/given/scope_selection';

vi.mock('../useHost', async () => {
    const { scopeSelectionSource } = await import('../../Views/for_DependencyMapView/given/scope_selection');
    return { useHost: () => ({
        model: { application: 'Work', documents: [{ path: 'work.play', source: scopeSelectionSource }] },
        refresh: vi.fn(), toggleFullscreen: vi.fn(),
    }) };
});

let host: HTMLDivElement;
let root: Root;
let board: Element;
let restore: () => void;

const settle = () => act(() => waitForAFrame());
const click = async (label: string) => { await act(async () => { scopeHeader(board, label).dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 1 })); }); await settle(); };
const pressEnter = async (label: string) => { await act(async () => { scopeHeader(board, label).dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true })); }); await settle(); };

beforeEach(async () => {
    prepareBoardDom();
    restore = layOutSliceColumns();
    host = document.createElement('div');
    document.body.appendChild(host);
    root = createRoot(host);
    await act(async () => root.render(createElement(BoardApp)));
    board = host.querySelector('.screenplay-board-view')!;
});
afterEach(async () => {
    await act(async () => root.unmount());
    host.remove();
    restore();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('when nothing is selected in the real MCP board', () => {
    it('should emphasise nothing and draw no slice links', () => {
        emphasisCount(board).should.equal(0);
        allSliceLinks(board).should.equal(0);
    });
});

describe('when selecting a slice in the real MCP board', () => {
    beforeEach(() => click('Slice: ShowFacts'));
    it('should select the slice and mark what it depends on', () => {
        emphasisOf(board, 'Slice: ShowFacts')!.should.equal('selected');
        emphasisOf(board, 'Slice: RecordFact')!.should.equal('dependency');
    });
    it('should say so in words', () => {
        scopeHeader(board, 'Slice: ShowFacts').closest('[data-slice-id]')!.getAttribute('aria-description')?.should.equal('Selected');
        board.querySelectorAll('[aria-description="Depended on by the selection"]').length.should.equal(1);
    });
    it('should draw the link across the module boundary as crossing', () => slicelinks(board, 'crossing').should.equal(1));
    it('should clear when the same header is selected again', async () => {
        await click('Slice: ShowFacts');
        emphasisCount(board).should.equal(0);
        allSliceLinks(board).should.equal(0);
    });
});

describe('when selecting a feature from the keyboard in the real MCP board', () => {
    beforeEach(() => pressEnter('Feature: Record'));
    it('should select the feature and mark what depends on it', () => {
        emphasisOf(board, 'Slice: RecordFact')!.should.equal('selected');
        emphasisOf(board, 'Slice: ShowFacts')!.should.equal('dependant');
    });
    it('should draw the slice link', () => slicelinks(board, 'crossing').should.equal(1));
});

describe('when selecting a module and then another scope in the real MCP board', () => {
    beforeEach(async () => { await click('Module: Views'); await click('Module: Facts'); });
    it('should follow the latest selection', () => {
        emphasisOf(board, 'Slice: RecordFact')!.should.equal('selected');
        emphasisOf(board, 'Slice: ShowFacts')!.should.equal('dependant');
    });
});
