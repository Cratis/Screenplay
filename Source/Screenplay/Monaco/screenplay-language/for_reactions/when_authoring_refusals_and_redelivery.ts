// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '@cratis/screenplay-compiler';
import { describe, expect, it } from 'vitest';
import type { Token } from 'monaco-editor';
import { createTokensProvider } from '../tokens';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';
import { scanDocument } from '../symbols';

const invocation = ['reaction R', '  when Approved', '    invokes Claim'];
const labels = (lines: string[], text: string) => {
    const plan = planCompletions([...lines, text], lines.length, text);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
};

describe('when authoring refusals and redelivery', () => {
    it('should retain contextual words as composite properties', () => {
        const names = ['events', 'refused', 'validation', 'authorization', 'acknowledge', 'redelivered'];
        const lines = ['type Details', ...names.map(name => `  ${name} String`)];
        expect(parse(lines.join('\n')).diagnostics).toEqual([]);
        expect(scanDocument(lines).types[0].properties.map(property => property.name)).toEqual(names);
    });
    it('should offer ordered refusal selectors inside an invocation', () => {
        expect(labels(invocation, '      ')).toEqual(expect.arrayContaining(['on refused', 'on refused by validation', 'on refused by constraint', 'on refused by authorization']));
    });
    it('should offer acknowledgement or event productions in a branch', () => {
        expect(labels([...invocation, '      on refused'], '        ')).toEqual(['acknowledge', 'produces']);
    });
    it('should offer refusal values only in their mapping scope', () => {
        expect(labels([...invocation, '      on refused by constraint Unique', '        produces Refused'], '          message = $refusal.')).toEqual(['reason', 'message', 'constraint']);
        expect(labels([...invocation, '      on refused by validation', '        produces Refused'], '          message = $refusal.')).toEqual(['reason', 'message']);
        expect(labels([...invocation, '      on refused', '        acknowledge'], '      message = $refusal.')).toEqual([]);
    });
    it('should offer redelivery as a specification action', () => {
        expect(labels(['specification Recovery'], '  ')).toContain('when redelivered');
        expect(labels(['specification Recovery'], '  when ')).toContain('redelivered');
    });
    it('should explain the authorization opt in and executable boundary', () => {
        const line = '      on refused';
        const content = hoverContent([...invocation, line], 3, 'refused', 10, 17)!;
        expect(content).toContain('not authorization');
        expect(content).toContain('not yet executable');
        const redelivery = '  when redelivered Approved to R';
        expect(hoverContent(['specification Recovery', redelivery], 1, 'redelivered', 8, 19)).toContain('not yet executable');
    });
    it('should explain refusal values within the branch', () => {
        const lines = [...invocation, '      on refused', '        produces Refused', '          reason = $refusal.reason'];
        const start = lines[5].lastIndexOf('reason') + 1;
        expect(hoverContent(lines, 5, 'reason', start, start + 6)).toContain('Refusal kind as a String');
    });
    it('should forward native refusal and redelivery diagnostics', () => {
        const lines = [...invocation, '      on refused', '        acknowledge'];
        for (const code of ['PLAY0538', 'PLAY0539', 'PLAY0540', 'PLAY0541', 'PLAY0542', 'PLAY0543', 'PLAY0544', 'PLAY0545']) {
            expect(validateLines(lines, { compilerDiagnostics: [{ code, severity: 'error', message: 'Invalid refusal', location: { line: 4, column: 7 } }] }).map(issue => issue.code)).toContain(code);
        }
    });
    it('should surface the callerless authorization warning at the branch', () => {
        const source = 'policy Access\n  require role "Manager"\nmodule Billing\n  feature Claims\n    slice Automation Claiming\n      event Approved\n      command Claim\n        authorize Access\n      reaction Claimer\n        when Approved\n          invokes Claim\n            on refused by authorization\n              acknowledge';
        const lines = source.split('\n');
        const native = parse(source).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0557');
        expect(native).toHaveLength(1);
        for (const context of [{}, { compilerDiagnostics: native }]) {
            expect(validateLines(lines, context).filter(issue => issue.code === 'PLAY0557')).toEqual([
                { code: 'PLAY0557', severity: 'warning', message: native[0].message, line: 11, startColumn: 13, endColumn: lines[11].length + 1 },
            ]);
        }
    });
    it('should highlight selectors acknowledgement redelivery and refusal values', async () => {
        const compilerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
        const lexerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
        const { compile } = await import(compilerPath);
        const { MonarchTokenizer } = await import(lexerPath);
        const tokenizer = new MonarchTokenizer({}, {}, 'screenplay', compile('screenplay', createTokensProvider([])), { getValue: () => 20000, onDidChangeConfiguration: () => ({ dispose() {} }) });
        try {
            for (const [line, word, type] of [
                ['runs as system role "Automation"', 'runs', 'keyword.play'],
                ['on refused by validation', 'refused', 'keyword.play'],
                ['on refused by authorization', 'authorization', 'keyword.play'],
                ['acknowledge', 'acknowledge', 'keyword.play'],
                ['when redelivered Approved to R', 'redelivered', 'keyword.play'],
                ['reason = $refusal.reason', '$refusal.reason', 'variable.predefined.play'],
                ['then events in any order', 'events', 'keyword.play'],
                ...['events', 'refused', 'validation', 'authorization', 'acknowledge', 'redelivered'].flatMap(word => [
                    [`${word} String`, word, 'identifier.play'],
                    [`${word} = "value"`, word, 'identifier.play'],
                ]),
            ]) {
                const tokens: Token[] = tokenizer.tokenize(line, true, tokenizer.getInitialState()).tokens;
                expect(tokens.filter(token => token.offset <= line.indexOf(word)).at(-1)!.type).toBe(type);
            }
        } finally { tokenizer.dispose(); }
    });
});
