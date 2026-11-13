// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { contextVariableEntries } from '../completion-items';
import { hoverContent } from '../hover-content';
import { keywordDocs } from '../keyword-docs';
import { moduleScopeItems, featureScopeItems } from '../scope-items';
import { sliceItems, topLevelItems } from '../completion-items';
import { providedValueAt, providedValuesAt } from '../provided-values';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

const document = [
    'provide region Region from host',
    'module M',
    '  provide region Region from query RegionById by $context.tenant',
    '  feature F',
    '    slice StateChange S',
    '      command C',
    '        name String',
    '      event E',
    '        name String',
    '      provide limit Int optional',
    '      produces E',
    '      authorize Reader when $context.region',
    '  feature G',
    '    slice StateChange T',
    '      command D',
    '        name String',
    '      provide zone Zone',
    '        file Zone.cs',
    '      authorize Reader when $context.zone',
];

const hoverOn = (line: number, word: string): string | null => {
    const start = document[line].indexOf(word) + 1;
    return hoverContent(document, line, word.replace(/^\$/, ''), start + 1, start + word.length, undefined);
};

describe('when authoring provided values', () => {
    it('should pick the nearest declaration of a name', () => {
        providedValueAt(document, 10, 'region')!.level.should.equal('module');
        providedValueAt(document, 10, 'region')!.source.should.equal('query');
    });

    it('should pick the application declaration where no nearer level declares it', () => {
        providedValueAt(['provide region Region from host', 'module M', '  feature F'], 2, 'region')!.level.should.equal('application');
    });

    it('should not carry a declaration into a sibling scope', () => {
        (providedValueAt(document, 18, 'limit') === undefined).should.be.true;
    });

    it('should flag host and code sources as opaque', () => {
        providedValueAt(document, 0, 'region')!.isOpaque.should.be.true;
        providedValueAt(document, 18, 'zone')!.isOpaque.should.be.true;
        providedValueAt(document, 10, 'region')!.isOpaque.should.be.false;
    });

    it('should list one winner per name in scope', () => {
        providedValuesAt(document, 10).map(value => value.name).sort().should.deep.equal(['limit', 'region']);
    });

    it('should show the winning declaration on hover', () => {
        const hover = hoverOn(11, '$context.region')!;
        hover.should.contain('module level');
        hover.should.contain('provide region Region from query RegionById');
    });

    it('should say an opaque value is never allowed or denied on hover', () => {
        hoverOn(18, '$context.zone')!.should.contain('Opaque');
    });

    it('should describe the provide keyword', () => {
        keywordDocs.provide.should.contain('nearest declaration wins');
    });

    it('should offer provided names where a context value is expected', () => {
        const labels = contextVariableEntries(scanDocument(document), document, 10).map(entry => entry.label);
        labels.should.include('$context.region');
        labels.should.include('$context.limit');
        labels.should.not.include('$context.zone');
        labels.should.include('$context.audiences');
    });

    it('should offer the provide declaration at every level', () => {
        for (const items of [topLevelItems, moduleScopeItems, featureScopeItems, sliceItems]) {
            items.map(item => item.label).should.include('provide from query');
        }
    });

    it('should not warn about a context path a provide names', () => {
        const lines = ['module M', '  provide region Region from host', '  feature F', '    slice StateChange S', '      command C', '        name String', '        require $context.region'];
        validateLines(lines).filter(issue => issue.code === 'PLAY0153').should.have.lengthOf(0);
    });

    it('should still warn about a context path no provide names', () => {
        const lines = ['module M', '  feature F', '    slice StateChange S', '      command C', '        name String', '        require $context.region'];
        validateLines(lines).filter(issue => issue.code === 'PLAY0153').should.have.lengthOf(1);
    });
});
