// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '@cratis/screenplay-compiler';

export const timelineFindingSource = 'module M\n  feature F\n    slice StateView View\n      readmodel P\n      projection P\n        from E\n    slice StateChange Write\n      event E';

// Keep the pinned board's own State View event warning: our information belongs to the slice header, not
// the event card, and both accessible markers remain visible when this projection reads from the right.
export function expectTimelineFindingOnTheConsumer(board: Element): void {
    const headers = [...board.querySelectorAll('.event-modeling-grid-slice-column-header')];
    const consumer = headers.find(header => header.textContent?.includes('View'))!;
    const producer = headers.find(header => header.textContent?.includes('Write'))!;
    (consumer !== undefined).should.be.true;
    (producer !== undefined).should.be.true;
    const badge = consumer.querySelector('.event-model-annotation-badge')!;
    (badge !== null).should.be.true;
    badge.getAttribute('data-severity')!.should.equal('information');
    const marker = badge.querySelector<HTMLElement>('[role="img"]')!;
    const label = marker.getAttribute('aria-label')!;
    label.should.contain('PLAY0516');
    label.should.contain(parse(timelineFindingSource, 'work.play').diagnostics[0].message);
    marker.getAttribute('title')!.should.equal(label);
    (producer.querySelector('.event-model-annotation-badge') === null).should.be.true;
    const event = board.querySelector('[data-out-of-order="true"]')!;
    (event !== null).should.be.true;
    event.querySelector('.event-model-annotation-badge')!.getAttribute('data-severity')!.should.equal('warning');
    event.querySelector('[role="img"]')!.getAttribute('aria-label')!.should.not.contain('PLAY0516');
}
