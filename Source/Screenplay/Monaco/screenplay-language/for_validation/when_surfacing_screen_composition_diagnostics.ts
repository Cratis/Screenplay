// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { diagnosticCodes } from '../diagnostic-codes';
import { createTokensProvider } from '../tokens';
import { constructKeywords } from '../language';
import { validateLines } from '../validation';

const composition = [
    'exposure for Shell',
    '  property "shell:header".title label "Header title"',
    '  property shellHeader.actions operations add, reorder fields label',
    '',
    'instance Browse',
    '  set "shell:header".title = "Invoices"',
    '  items shellHeader.actions',
    '    item "export:csv"',
    '      label = "Export"',
    '',
    'module Sales',
    '  screen template Shell',
    '    display "Sales shell"',
    '    scopes slice',
    '    header contributes Actions',
    '    body',
    '',
    '    content header',
    '      title "Sales"',
    '',
    '    arrangement flow',
    '      grid gap 8 columns 2 rows 1 grow',
    '        header span 2',
    '        body grow 2',
    '',
    '  feature Invoices',
    '    slice StateView Browse',
    '      screen Browse',
    '        template Shell',
    '        contribute to Actions order 10',
    '          title "Register"',
];

const malformed = [
    'exposure Shell',
    '  property title',
    'instance',
    '  put shellHeader.title = 1',
    'module Sales',
    '  feature Invoices',
    '    slice StateView Browse',
    '      screen Browse',
    '        contribute Actions',
    '        navigate to Browse',
    '          outlet',
];

const compositionCodes = [
    'PLAY0621', 'PLAY0622', 'PLAY0623', 'PLAY0624', 'PLAY0625', 'PLAY0626', 'PLAY0627', 'PLAY0628',
    'PLAY0629', 'PLAY0630', 'PLAY0631', 'PLAY0632', 'PLAY0654', 'PLAY0655', 'PLAY0656',
];

describe('when the editor reads well-formed screen composition', () => {
    it('should parse it with the shared compiler', () => parse(composition.join('\n')).diagnostics.should.be.empty);
    it('should report no editor issues', () => validateLines(composition).filter(issue => issue.severity === 'error').should.be.empty);
    it('should register the top-level keywords', () => constructKeywords.should.include.members(['exposure', 'instance']));
    it('should highlight the composition keywords', () => createTokensProvider([]).keywords.should.include.members(['exposure', 'instance', 'display', 'scopes', 'reexposes', 'operations', 'items']));
});

describe('when the editor reads malformed screen composition', () => {
    const issues = validateLines(malformed);

    it('should mark the malformed exposure', () => issues.filter(issue => issue.code === 'PLAY0621').map(issue => issue.line + 1).should.deep.equal([1, 2]));
    it('should mark the malformed instance', () => issues.filter(issue => issue.code === 'PLAY0625').map(issue => issue.line + 1).should.deep.equal([3, 4]));
});

describe('when a Monaco host supplies screen composition diagnostics from the native compiler', () => {
    it.each(compositionCodes)('should surface %s as a marker', code => {
        const supplied = { code, severity: 'error' as const, message: `Native ${code}`, location: { line: 1, column: 1 } };
        validateLines(['module M'], { compilerDiagnostics: [supplied] }).filter(issue => issue.code === code).map(issue => issue.message).should.deep.equal([supplied.message]);
    });
    it('should register every composition code', () => compositionCodes.every(code => Object.values(diagnosticCodes).some(registered => registered === code)).should.be.true);
});
