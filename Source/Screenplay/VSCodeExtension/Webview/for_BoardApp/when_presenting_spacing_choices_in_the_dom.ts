// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { act, createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { compileEventModelApplication, dependencyMapFor, toEventModelDocument } from '@cratis/screenplay-event-models';
import type { BoardDensity, BoardTextSize } from '@cratis/event-models';
import { BoardApp } from '../BoardApp';
import type { BoardViewOptions } from '../BoardMessage';
import { vscode } from '../vscodeApi';
import { activateWithKeyboard, prepareBoardDom, toolbarButton } from '../../../Views/for_DependencyMapView/given/real_board_dom';

vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));

let host: HTMLDivElement;
let root: Root;
const options: BoardViewOptions = { detailLevel: 'full', showProperties: false, visualizationMode: 'simplified', density: 'comfortable', textSize: 'medium' };
const application = compileEventModelApplication([{ path: 'work.play', source: 'module Work\n  feature Record\n    slice StateChange RecordWork\n      event WorkRecorded' }]).value;

async function present(changes: Partial<BoardViewOptions>) {
    await act(async () => window.dispatchEvent(new MessageEvent('message', { data: { type: 'viewOptions', options: { ...options, ...changes } } })));
}

function renderedBoard() {
    const board = host.querySelector<HTMLElement>('[data-board-density]');
    (board !== null).should.be.true;
    return board!;
}

beforeEach(async () => {
    prepareBoardDom();
    vi.mocked(vscode.postMessage).mockClear();
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

describe('when presenting spacing choices in the real VS Code board', () => {
    it('should initially render comfortable spacing and medium text', () => {
        renderedBoard().getAttribute('data-board-density')!.should.equal('comfortable');
        renderedBoard().style.getPropertyValue('--board-text-scale').should.equal('1');
    });

    for (const density of ['compact', 'comfortable', 'spacious'] satisfies BoardDensity[]) {
        it(`should render ${density} density without replacing the canvas`, async () => {
            const canvas = host.querySelector('.canvas-surface');
            (canvas !== null).should.be.true;
            await present({ density });
            renderedBoard().getAttribute('data-board-density')!.should.equal(density);
            (host.querySelector('.canvas-surface') === canvas).should.be.true;
        });
    }

    for (const [textSize, scale] of [['small', '0.9'], ['medium', '1'], ['large', '1.25']] satisfies [BoardTextSize, string][]) {
        it(`should render ${textSize} text at scale ${scale}`, async () => {
            await present({ textSize });
            renderedBoard().style.getPropertyValue('--board-text-scale').should.equal(scale);
        });
    }

    it('should not echo received options as an outgoing save', async () => {
        vi.mocked(vscode.postMessage).mockClear();
        await present({ density: 'spacious', textSize: 'large' });
        vi.mocked(vscode.postMessage).mock.calls.should.have.lengthOf(0);
    });

    it('should keep configured spacing and text size after changing Properties in the native View menu', async () => {
        await present({ density: 'spacious', textSize: 'large' });
        vi.mocked(vscode.postMessage).mockClear();
        await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-board-view')!, 'View'));
        const properties = [...document.body.querySelectorAll<HTMLButtonElement>('.event-modeling-menu-dropdown-menu button')]
            .find(button => button.textContent?.trim() === 'Properties');
        (properties !== undefined).should.be.true;
        await activateWithKeyboard(properties!);
        vi.mocked(vscode.postMessage).mock.calls.should.deep.equal([[{
            type: 'viewOptionsChanged', options: { ...options, showProperties: true, density: 'spacious', textSize: 'large' },
        }]]);
        renderedBoard().getAttribute('data-board-density')!.should.equal('spacious');
        renderedBoard().style.getPropertyValue('--board-text-scale').should.equal('1.25');
    });
});
