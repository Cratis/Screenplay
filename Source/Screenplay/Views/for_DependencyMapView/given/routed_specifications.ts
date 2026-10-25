// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'vitest';

// One slice with a specification whose title is long but route-free, beside one that routes its event to a stream.
export const routedSpecificationSource = [
    'eventsource A',
    '  identifier String',
    '  stream S',
    '    streamId',
    '      projectId String',
    '      period String',
    'module M',
    '  feature F',
    '    slice StateChange S',
    '      command C',
    '        projectId String identifier',
    '        month String',
    '      event E',
    '      specification ReadingTheQuarterlyCustomerStatementWithAnExceptionallyLongTitleThatNeverEnds',
    '        when C',
    '          projectId = "p"',
    '          month = "2026-10"',
    '        then E',
    '      specification Routed',
    '        when C',
    '          projectId = "p"',
    '          month = "2026-10"',
    '        then E',
    '          stream A.S',
    '            streamId',
    '              period = "<2026 & 10>"',
    '              projectId = "p"',
].join('\n');

function headerNamed(board: Element, title: string): Element {
    const header = [...board.querySelectorAll('.event-modeling-grid-specification-header')].find(candidate => candidate.textContent?.includes(title));
    if (header === undefined) throw new Error(`Missing specification header: ${title}`);
    return header;
}

// What the real board shows for the sources above: routing only in the information badges of the routed specification
// and of its slice, whose header summarizes every annotation in the slice.
export function expectRoutesOnlyInTheBadge(board: Element): void {
    const routed = headerNamed(board, 'Routed');
    const plain = headerNamed(board, 'Reading The Quarterly');
    expect(routed).not.toBe(plain);
    expect(routed.textContent).not.toContain('stream A.S');
    expect(plain.textContent).not.toContain('stream');
    expect(plain.textContent).toContain('Exceptionally Long Title That Never Ends');
    expect(plain.querySelector('.event-model-annotation-badge')).toBeNull();
    const badge = routed.querySelector<HTMLElement>('.event-model-annotation-badge [role="img"]');
    expect(badge).not.toBeNull();
    expect(badge!.closest('.event-model-annotation-badge')!.getAttribute('data-severity')).toBe('information');
    const label = badge!.getAttribute('aria-label')!;
    expect(label).toContain('then 1: E — stream A.S; streamId period = "<2026 & 10>", projectId = "p"');
    expect(label).toContain('Admitted by ESM v8');
    expect(badge!.getAttribute('title')).toBe(label);
    const slice = board.querySelector<HTMLElement>('.event-modeling-grid-slice-column-header .event-model-annotation-badge');
    expect(slice).not.toBeNull();
    expect(slice!.getAttribute('data-severity')).toBe('information');
    expect(slice!.querySelector('[role="img"]')!.getAttribute('aria-label')).toContain('then 1: E — stream A.S');
}
