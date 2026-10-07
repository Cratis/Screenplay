// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { dependencyMapFor } from '@cratis/screenplay-event-models';
import { DependencyMapView } from '../DependencyMapView';

const map = dependencyMapFor(parse(`module Timesheets
  feature Record
    slice StateChange RecordTime
      event TimeRecorded
module Payroll
  feature Pay
    slice StateView ShowPay
      readmodel Pay
      projection Pay
        from TimeRecorded
`).value);

describe('when rendering a dependency map', () => {
    const edge = map.edges.find(edge => edge.source === 'module:Payroll')!;
    const markup = renderToStaticMarkup(createElement(DependencyMapView, { map, selectedEdgeId: edge.id }));
    it('should label keyboard-focusable edges', () => markup.should.contain('aria-label="Payroll uses facts from Timesheets, 1 slice" tabindex="0"'));
    it('should label keyboard-focusable nodes', () => markup.should.contain('aria-label="Module Payroll" tabindex="0"'));
    it('should provide a live edge detail panel', () => markup.should.contain('aria-live="polite"'));
    it('should list the evidence behind the selected edge', () => markup.should.contain('Payroll.Pay.ShowPay → Timesheets.Record.RecordTime'));
    it('should provide a screen-reader edge table', () => markup.should.contain('<caption>All dependencies</caption>'));
    it('should hide non-ordering kinds by default', () => markup.should.contain('value="outsideTheModel"'));
});
