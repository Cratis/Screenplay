// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { completenessDiagnostics, parse } from '@cratis/screenplay-compiler';
import { describe, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { createTokensProvider } from '../tokens';
import { validateLines } from '../validation';

const lines = ['concept Status : Enum', '  draft = -1', '  @validate = 4'];

describe('when authoring enum member numbers', () => {
    it('should complete an explicitly numbered member', () => {
        const plan = planCompletions([lines[0], '  '], 1, '  ');
        (plan.kind === 'entries' && plan.entries.some(entry => entry.insertText === '${1:value} = ${2:0}')).should.equal(true);
    });
    it('should show names and numbers in concept hover', () => {
        hoverContent(lines, 0, 'Status', 9, 15)!.should.contain('draft = -1, validate = 4');
    });
    it('should show Unicode member names with their numbers', () => {
        hoverContent(['concept Status : Enum', '  café = 1', '  søk = 4'], 0, 'Status', 9, 15)!.should.contain('café = 1, søk = 4');
    });
    it('should keep validation directives out of member hover', () => {
        hoverContent([...lines, '  validate', '    not empty'], 0, 'Status', 9, 15)!.should.contain('Values: draft = -1, validate = 4');
    });
    it('should highlight the name, equals and number separately', () => {
        const rule = createTokensProvider([]).tokenizer.conceptBody.find(rule => Array.isArray(rule) && String(rule[0]).includes('[0-9]')) as [RegExp, string[]];
        rule[0].test(lines[2]).should.equal(true);
        rule[0].test('  café = 4').should.equal(true);
        rule[1].should.deep.equal(['white', 'identifier', 'white', 'operator', 'white', 'number']);
    });
    it.each([
        [['concept Status : Enum', '  draft = 0', '  issued'], 'PLAY0667'],
        [['concept Status : Enum', '  draft = 0', '  issued = 0'], 'PLAY0668'],
        [['concept Status : Enum', '  draft = 2147483648'], 'PLAY0669'],
        [['concept Status : Enum', '  draft = 1e0'], 'PLAY0670'],
    ])('should surface shared numbering errors %s', (source, code) => {
        validateLines(source as string[]).some(issue => issue.code === code && issue.severity === 'error').should.equal(true);
    });
    it('should retain the stored-enum information hint only when completeness findings are supplied', () => {
        const source = ['concept Status : Enum', '  draft', 'module M', '  feature F', '    slice StateChange S', '      event E', '        status Status'];
        validateLines(source).filter(issue => issue.code === 'PLAY0671').should.have.lengthOf(0);
        const compilerDiagnostics = completenessDiagnostics(parse(source.join('\n')), ['enum-pinning']);
        validateLines(source, { compilerDiagnostics }).filter(issue => issue.code === 'PLAY0671').map(issue => issue.severity).should.deep.equal(['information']);
    });
});
