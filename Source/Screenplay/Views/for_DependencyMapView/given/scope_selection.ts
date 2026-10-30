// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const scopeSelectionSource = `module Facts
  feature Record
    slice StateChange RecordFact
      event FactRecorded
module Views
  feature Display
    slice StateView ShowFacts
      readmodel Facts
      projection Facts
        from FactRecorded
`;

// happy-dom measures nothing, so the board has no anchors to draw slice links between. Lay the slice
// columns out left to right, the way the board does, for as long as the spec runs.
export function layOutSliceColumns(): () => void {
    const measure = HTMLElement.prototype.getBoundingClientRect;
    HTMLElement.prototype.getBoundingClientRect = function (this: HTMLElement): DOMRect {
        const columns = [...document.querySelectorAll('[data-slice-id]')];
        const index = columns.indexOf(this);
        if (index < 0) return measure.call(this);
        return { x: index * 200, y: 100, left: index * 200, top: 100, width: 150, height: 300, right: index * 200 + 150, bottom: 400, toJSON: () => ({}) };
    };
    return () => { HTMLElement.prototype.getBoundingClientRect = measure; };
}

export function scopeHeader(board: Element, label: string): HTMLElement {
    const header = board.querySelector<HTMLElement>(`[aria-label="${label}"]`);
    if (!header) throw new Error(`Missing scope header: ${label}`);
    return header;
}

export function emphasisOf(board: Element, label: string): string | undefined {
    const header = scopeHeader(board, label);
    const holder = header.closest('[data-slice-id]') ?? header;
    return ['selected', 'dependency', 'dependant', 'dimmed'].find(kind => holder.classList.contains(`event-model-emphasis--${kind}`));
}

export const emphasisCount = (board: Element) => board.querySelectorAll('.event-model-emphasis').length;
export const slicelinks = (board: Element, emphasis: 'within' | 'crossing') => board.querySelectorAll(`[data-slice-link="${emphasis}"]`).length;
export const allSliceLinks = (board: Element) => board.querySelectorAll('[data-slice-link]').length;

// The board draws its links a few frames after the selection: the host asks it to measure on the next frame,
// and it measures its slice columns on the one after.
export async function waitForAFrame(): Promise<void> {
    for (let frame = 0; frame < 3; frame++) await new Promise(resolve => requestAnimationFrame(() => resolve(undefined)));
}
