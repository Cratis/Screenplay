// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';
import { sliceItemsFor } from '../completion-items';
import { keywordDocs } from '../keyword-docs';
import { validateLines } from '../validation';
import { scanDocument } from '../symbols';
import { hoverContent } from '../hover-content';
import { EventVisibility } from '@cratis/screenplay-compiler';

const prefix = ['module M', '  feature F', '    slice Translate S'];

describe('when authoring public event metadata', () => {
    it('should offer directions only within Translate slices', () => {
        sliceItemsFor('Translate').some(item => item.label === 'direction').should.be.true;
        for (const type of ['StateChange', 'StateView', 'Automation']) sliceItemsFor(type).some(item => item.label === 'direction').should.be.false;
    });
    it('should offer public event headers and event body directives', () => {
        sliceItemsFor('Translate').some(item => item.label === 'public event').should.be.true;
        completionEntriesFor(['public', 'slice']).some(item => item.label === 'id').should.be.true;
    });
    it('should explain the explicit semantic refusal', () => {
        keywordDocs.public.should.contain('PLAY0268');
        keywordDocs.direction.should.contain('PLAY0268');
    });
    it('should accept authoring syntax without claiming executable admission', () => {
        validateLines([...prefix, '      direction inbound', '      public event Published from "billing"', '        number String']).should.deep.equal([]);
    });
    it('should preserve imported public events in source symbols and hover', () => {
        const lines = ['import Billing.Issued from "billing/Issued.play"', ...prefix, '      public event Published generation 2 from "billing/Issued.play"', '        number String'];
        const symbols = scanDocument(lines);
        symbols.events[0].visibility!.should.equal(EventVisibility.Public);
        symbols.events[0].origin!.should.equal('billing/Issued.play');
        symbols.events[0].properties.map(property => property.name).should.deep.equal(['number']);
        symbols.imports[0].qualifiedName.should.equal('Billing.Issued');
        hoverContent(lines, 4, 'Published', 20, 29)!.should.contain('public event Published generation 2 from "billing/Issued.play"');
    });
    it('should diagnose invalid directions through the compiler', () => {
        validateLines([...prefix, '      direction sideways']).map(issue => issue.code).should.contain('PLAY0027');
    });
});
