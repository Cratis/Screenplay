// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, createElement } from 'react';
import { createRoot } from 'react-dom/client';
import { compileEventModelApplication } from '@cratis/screenplay-event-models';
import { boardFor } from '../../../EventModelBoard/compileBoard';
import { BoardApp } from '../../BoardApp';
import type { ShowBoardMessage } from '../../BoardMessage';
import { prepareBoardDom } from '../../../../Views/for_DependencyMapView/given/real_board_dom';

// A one-slice model whose role has a screen, which is what the board is asked to draw.
export const a_model_with_a_screen = [
    'module Sales',
    '  feature Checkout',
    '    slice StateView Orders',
    '      query AllOrders => Order[]',
    '      screen Listing',
    '        title "Orders"',
].join('\n');

// The same model, whose screen says nothing a Scene document can hold.
export const a_model_with_an_undrawable_screen = [
    'module Sales',
    '  feature Checkout',
    '    slice StateView Orders',
    '      query AllOrders => Order[]',
    '      screen Listing',
].join('\n');

export const a_board = (source: string): ShowBoardMessage =>
    boardFor(compileEventModelApplication([{ path: 'sales.play', source }]), 'Sales');

export interface ShownBoard {
    readonly host: HTMLDivElement;
    readonly close: () => Promise<void>;
}

// The webview, shown the given board, in a DOM close enough to the real one to draw it.
export async function a_shown_board(message: ShowBoardMessage): Promise<ShownBoard> {
    prepareBoardDom();
    const host = document.createElement('div');
    document.body.appendChild(host);
    const root = createRoot(host);
    await act(async () => root.render(createElement(BoardApp)));
    await act(async () => window.dispatchEvent(new MessageEvent('message', { data: message })));
    return {
        host,
        close: async () => {
            await act(async () => root.unmount());
            host.remove();
        },
    };
}
