// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from 'react';
import { vi } from 'vitest';
import { Application } from 'pixi.js';

export function prepareBoardDom() {
    // Keep the real board, canvas DOM, grid, toolbar and body portals. Only the GPU renderer is
    // substituted: happy-dom has no WebGL. The board uses HTML items, not Pixi sprites.
    vi.spyOn(Application.prototype, 'init').mockImplementation(async function (this: Application) {
        this.renderer = {
            canvas: document.createElement('canvas'), render: vi.fn(), resize: vi.fn(),
        } as unknown as Application['renderer'];
    });
    vi.spyOn(Application.prototype, 'destroy').mockImplementation(function (this: Application) {
        this.canvas.remove();
        this.stage.destroy({ children: true });
    });
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true);
    // The package's documented no-observer fallback mounts the grid. happy-dom does not
    // calculate layout intersections, so its no-op observer would otherwise hide the grid.
    vi.stubGlobal('IntersectionObserver', undefined);
}

export async function activateWithKeyboard(button: HTMLButtonElement) {
    await act(async () => {
        button.focus();
        button.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        // DOM emulators do not perform the browser's native Enter -> click default action.
        // detail: 0 is keyboard activation; deliberately do not send pointerdown.
        button.dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 0 }));
        button.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', bubbles: true }));
    });
}

export function toolbarButton(container: Element, label: string) {
    const button = container.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);
    if (!button) throw new Error(`Missing toolbar button: ${label}`);
    return button;
}
