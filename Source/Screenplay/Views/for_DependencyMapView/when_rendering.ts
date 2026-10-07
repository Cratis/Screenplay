// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { describe, it } from 'vitest';
import { dependencyKinds, parse } from '@cratis/screenplay-compiler';
import { dependencyMapFor } from '@cratis/screenplay-event-models';
import { DependencyMapView } from '../DependencyMapView';

const map = dependencyMapFor(parse(`module Timesheets
  feature Record
    slice StateChange RecordTime
      event TimeRecorded
      event MoreTimeRecorded
module Payroll
  feature Pay
    slice StateView ShowPay
      readmodel Pay
      projection Pay
        from TimeRecorded
        from MoreTimeRecorded
`).value);

describe('when rendering a dependency map', () => {
    const edge = map.edges.find(edge => edge.source === 'module:Payroll')!;
    const markup = renderToStaticMarkup(createElement(DependencyMapView, { map, selectedEdgeId: edge.id }));
    it('should label keyboard-focusable edges', () => markup.should.contain('aria-label="Payroll uses facts from Timesheets, 1 slice pair" tabindex="0"'));
    it('should give the drawing only one Tab stop', () => (markup.match(/tabindex="0"/g) ?? []).length.should.equal(1));
    it('should keep other map items out of the Tab order', () => markup.should.contain('aria-label="Module Payroll" tabindex="-1"'));
    it('should announce only a short selection status', () => markup.should.contain('role="status" aria-live="polite" aria-atomic="true">Payroll uses facts from Timesheets, 1 slice pair selected</p>'));
    it('should not make the evidence panel live', () => markup.should.not.match(/<aside[^>]*aria-live/));
    it('should list the evidence behind the selected edge', () => markup.should.contain('Payroll.Pay.ShowPay → Timesheets.Record.RecordTime'));
    it('should provide a screen-reader edge table', () => markup.should.contain('<caption>All dependencies</caption>'));
    it('should group references under a single slice-pair row', () => (markup.match(/class="screenplay-dependency-map__slice-pair"/g) ?? []).length.should.equal(1));
    it('should nest every reference below its slice pair', () => markup.should.contain('<ul><li>uses facts from: TimeRecorded'));
    it('should cap line labels to counts while retaining the full title', () => {
        markup.should.contain('<title>uses facts from 2</title>');
        markup.should.match(/<text[^>]*text-anchor="middle"[^>]*>1<\/text>/);
    });
    it('should check all ordering kinds by default', () => ['usesFactsFrom', 'reactsTo', 'decidesFrom'].every(kind => (markup.match(/<input[^>]*>/g) ?? []).some(input => input.includes(`value="${kind}"`) && input.includes('checked=""'))).should.be.true);
    it('should leave every other kind unchecked by default', () => dependencyKinds.filter(kind => !['usesFactsFrom', 'reactsTo', 'decidesFrom'].includes(kind)).every(kind => (markup.match(/<input[^>]*>/g) ?? []).some(input => input.includes(`value="${kind}"`) && !input.includes('checked=""'))).should.be.true);
});
