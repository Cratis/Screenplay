// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';
import { contextVariableEntries } from '../completion-items';
import { scanDocument } from '../symbols';
import { keywordDocs } from '../keyword-docs';
import { blockHeaderPattern, languageConfiguration } from '../language';
import { validateLines } from '../validation';

const declaration = ['context', '  bookingZone time zone'];

describe('when authoring application context', () => {
    it('should indent a context block but not a property called context', () => {
        for (const pattern of [blockHeaderPattern, languageConfiguration.indentationRules!.increaseIndentPattern]) {
            pattern.test('context').should.be.true;
            pattern.test('context // inputs').should.be.true;
            pattern.test('        context String').should.be.false;
        }
    });

    it('should offer a single application-root context block', () => {
        completionEntriesFor([]).find(entry => entry.label === 'context')!.insertText.should.contain('time zone');
        completionEntriesFor([], { headers: [], document: declaration }).map(entry => entry.label).should.not.contain('context');
        completionEntriesFor(['module']).map(entry => entry.label).should.not.contain('context');
        completionEntriesFor(['feature']).map(entry => entry.label).should.not.contain('context');
    });

    it('should complete only supported declaration forms within the block', () => {
        const entries = completionEntriesFor(['context']);
        entries.map(entry => entry.label).should.deep.equal(['time zone member', 'description']);
        entries[0].insertText.should.contain('time zone');
        entries[0].documentation!.should.contain('ESM v37');
        keywordDocs.context.should.contain('$context.timeZone');
    });

    it('should not advertise a new executable context root', () => {
        contextVariableEntries(scanDocument(declaration)).map(entry => entry.label).should.not.contain('$context.bookingZone');
    });

    it('should accept context declarations without unknown-construct diagnostics', () => {
        validateLines(declaration).should.deep.equal([]);
    });

    it.each<[string[], string]>([
        [['context Named'], 'PLAY0854'],
        [['context', '  zone String'], 'PLAY0855'],
        [['context', '  zone time zone', '  @zone time zone'], 'PLAY0856'],
        [['context', '  @identity time zone'], 'PLAY0857'],
    ])('should retain compiler diagnostics for %s', (lines, code) => {
        validateLines(lines).filter(issue => issue.code === code).should.have.lengthOf(1);
    });

    it('should leave contextual property names valid', () => {
        validateLines(['module M', '  feature F', '    slice StateChange S', '      command C', '        context String', '        zone String', '        timeZone String']).should.deep.equal([]);
    });
});
