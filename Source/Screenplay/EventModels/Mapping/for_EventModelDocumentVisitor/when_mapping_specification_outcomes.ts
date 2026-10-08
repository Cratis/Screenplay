// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { readEventModelDocument, SpecificationHeader, SpecificationRunState } from '@cratis/event-models';
import { EventModelDocument, SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';

const source = `
concept ReceiptId : Uuid
module Projects
  feature Registration
    slice StateChange Register
      command RegisterProject
        projectId Uuid identifier
        receiptId ReceiptId generated
        name String
        returns receiptId
      specification RegistrationReturnsReceipt
        when RegisterProject
          projectId = "11111111-1111-1111-1111-111111111111"
          generated receiptId = "22222222-2222-2222-2222-222222222222"
          name = "Apollo"
        then returns "22222222-2222-2222-2222-222222222222"
      // An undeclared command models a partial draft; its authored expressions still appear on the board.
      specification RegistrationReturnsRecord
        when PreviewRegistration
          generated receiptId = "<receipt & id>"
          generated retry = false
          generated count = 0
          name = "<Apollo & Artemis>"
        then returns
          receiptId = "<receipt & id>"
          timestamp = "2026-10-07"
      specification NoProjectRemains
        when RegisterProject
          name = "Apollo"
        then no readmodel Project for "11111111-1111-1111-1111-111111111111"
        then no readmodel ProjectSummary for { "id": 42 }
      specification NoAdditiveMembers
        when RegisterProject
          name = "Apollo"
        then error
`;

function headerOf(document: EventModelDocument, index: number): string {
    const specification = readEventModelDocument(document).collections[0].modules[0].features[0].slices[0].specifications[index];
    return renderToStaticMarkup(createElement(SpecificationHeader, {
        name: specification.name, collapsed: specification.collapsed, runState: SpecificationRunState.Idle,
    }));
}

describe('when mapping specification outcomes', () => {
    let document: EventModelDocument;
    let specifications: SliceSpecificationDocument[];

    beforeAll(() => {
        const result = parse(source);
        result.diagnostics.should.deep.equal([]);
        document = toEventModelDocument(result.value, 'Projects');
        specifications = document.collections[0].modules[0].features[0].slices[0].specifications;
    });

    it('should keep generated fixtures separate from request values', () => {
        specifications[0].when!.generatedValues!.should.deep.equal({ receiptId: '22222222-2222-2222-2222-222222222222' });
        specifications[0].when!.values.should.deep.equal({ projectId: '11111111-1111-1111-1111-111111111111', name: 'Apollo' });
    });
    it('should carry scalar and record return assertions', () => {
        specifications[0].thenReturns!.should.deep.equal({ value: '22222222-2222-2222-2222-222222222222' });
        specifications[1].thenReturns!.should.deep.equal({ fields: { receiptId: '<receipt & id>', timestamp: '2026-10-07' } });
    });
    it('should carry absence assertions with their read model names and keys', () => {
        specifications[2].thenAbsentReadModels!.should.deep.equal([
            { name: 'Project', key: '11111111-1111-1111-1111-111111111111' },
            { name: 'ProjectSummary', key: '{ "id": 42 }' },
        ]);
    });
    it('should render generated fixtures and scalar returns in the published board header', () => {
        const markup = headerOf(document, 0);
        markup.should.contain('Registration Returns Receipt — generated (not request inputs): receiptId = &quot;22222222-2222-2222-2222-222222222222&quot;');
        markup.should.contain('then returns &quot;22222222-2222-2222-2222-222222222222&quot;');
    });
    it('should render falsy fixtures and escape record values without treating them as HTML', () => {
        const markup = headerOf(document, 1);
        markup.should.contain('generated (not request inputs): receiptId = &quot;&lt;receipt &amp; id&gt;&quot;, retry = false, count = 0');
        markup.should.contain('then returns { receiptId = &quot;&lt;receipt &amp; id&gt;&quot;, timestamp = &quot;2026-10-07&quot; }');
        markup.should.not.contain('<receipt & id>');
    });
    it('should render every absence assertion in the published board header', () => {
        const markup = headerOf(document, 2);
        markup.should.contain('then no readmodel Project for &quot;11111111-1111-1111-1111-111111111111&quot;');
        markup.should.contain('then no readmodel ProjectSummary for { &quot;id&quot;: 42 }');
    });
    it('should remain readable by the published board schema', () => {
        problems_the_board_finds_in(document).should.deep.equal([]);
    });
    it('should omit additive members and keep titles unchanged for old specifications', () => {
        specifications[3].name.should.equal('NoAdditiveMembers');
        (specifications[3].when!.generatedValues === undefined).should.be.true;
        (specifications[3].thenReturns === undefined).should.be.true;
        (specifications[3].thenAbsentReadModels === undefined).should.be.true;
    });
});
