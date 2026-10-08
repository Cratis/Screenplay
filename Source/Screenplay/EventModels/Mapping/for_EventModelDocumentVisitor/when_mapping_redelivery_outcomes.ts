// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { beforeAll, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { readEventModelDocument, SpecificationHeader, SpecificationRunState } from '@cratis/event-models';
import { EventModelDocument, SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';

const source = readFileSync(new URL('../../../Compiler/Conformance/reaction-refusals-redelivery.play', import.meta.url), 'utf8').replace('numbers exact\n', '') + `
      specification UnhandledRefusal
        given Approved
          invoice = "invoice-1"
        when redelivered Approved to Claimer
          invoice = "invoice-1"
        then error "<Claim & validation>"
      specification Unauthorized
        given Approved
          invoice = "invoice-1"
        when redelivered Approved to Claimer
        then denied
      specification AcceptedRefusalFact
        given Approved
          invoice = "invoice-1"
        when redelivered Approved to Claimer
        then Refused
          reason = "constraint"
          constraint = "UniqueClaim"
          message = "Already claimed"
      specification NoNewFactsOnStartup
        when trigger Startup
        then no events
`;

function headerOf(document: EventModelDocument, index: number): string {
    const specification = readEventModelDocument(document).collections[0].modules[0].features[0].slices[0].specifications[index];
    return renderToStaticMarkup(createElement(SpecificationHeader, {
        name: specification.name, collapsed: specification.collapsed, runState: SpecificationRunState.Idle,
    }));
}

describe('when mapping redelivery outcomes', () => {
    let document: EventModelDocument;
    let specifications: SliceSpecificationDocument[];

    beforeAll(() => {
        const result = parse(source);
        result.diagnostics.should.deep.equal([]);
        document = toEventModelDocument(result.value, 'Billing');
        specifications = document.collections[0].modules[0].features[0].slices[0].specifications;
    });

    it('should show redelivery as its own action with the reaction and occurrence selector', () => {
        specifications[0].when!.name.should.equal('redelivered Approved to Claimer for "invoice-1"');
        specifications[0].when!.values.should.deep.equal({ invoice: 'invoice-1' });
        (specifications[0].when!.commandId === undefined).should.be.true;
        specifications[0].whenRedelivered!.should.deep.equal({
            eventType: 'Approved', reaction: 'Claimer', for: 'invoice-1', values: { invoice: 'invoice-1' },
        });
    });
    it('should retain the given fact rather than add it as an appended or expected event', () => {
        specifications[0].given.should.have.lengthOf(1);
        specifications[0].thenEvents.should.deep.equal([]);
        specifications[0].thenErrors.should.deep.equal([]);
    });
    it('should distinguish explicit no-event expectations from an empty list of expected events', () => {
        specifications[0].thenNoEvents!.should.be.true;
        specifications[4].thenNoEvents!.should.be.true;
        (specifications[1].thenNoEvents === undefined).should.be.true;
    });
    it('should render redelivery and no-event intent with an explicit admission boundary', () => {
        const markup = headerOf(document, 0);
        markup.should.contain('when redelivered Approved to Claimer for &quot;invoice-1&quot;');
        markup.should.contain('then no events');
        markup.should.contain('Syntax-only (PLAY0268)');
        markup.should.contain('not executable assertions');
    });
    it('should render no-event expectations for other actions too', () => {
        headerOf(document, 4).should.contain('then no events');
        specifications[4].when!.name.should.equal('trigger Startup');
        (specifications[4].whenRedelivered === undefined).should.be.true;
    });
    it('should preserve unhandled refusal messages and distinguish authorization denials', () => {
        specifications[1].thenErrors.map(error => ({ name: error.name, message: error.message })).should.deep.equal([
            { name: 'error', message: '<Claim & validation>' },
        ]);
        specifications[2].thenErrors.map(error => error.name).should.deep.equal(['denied']);
        (specifications[2].whenRedelivered!.for === undefined).should.be.true;
        specifications[2].when!.values.should.deep.equal({});
    });
    it('should retain the expected refusal event payload instead of treating it as a rejection', () => {
        specifications[3].thenEvents[0].values.should.deep.equal({ reason: 'constraint', constraint: 'UniqueClaim', message: 'Already claimed' });
        specifications[3].thenErrors.should.deep.equal([]);
    });
    it('should remain readable by the published board schema', () => {
        problems_the_board_finds_in(document).should.deep.equal([]);
    });
    it('should escape opaque selector text in the published header', () => {
        const escaped = toEventModelDocument(parse(source.replaceAll('invoice-1', '<invoice & id>')).value, 'Billing');
        const markup = headerOf(escaped, 0);
        markup.should.contain('for &quot;&lt;invoice &amp; id&gt;&quot;');
        markup.should.not.contain('<invoice & id>');
    });
});
