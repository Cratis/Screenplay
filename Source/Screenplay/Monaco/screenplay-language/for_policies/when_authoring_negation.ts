// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { Token } from 'monaco-editor';
import { createTokensProvider } from '../tokens';
import { planCompletions } from '../completion-planner';
import { keywordDocs } from '../keyword-docs';
import { clauseKeywords } from '../language';
import { validateLines } from '../validation';

describe('when authoring policy negation', () => {
    it('should offer exclusions inside a policy', () => {
        const plan = planCompletions(['policy PersonOnly', '  '], 1, '  ');
        expect(plan.kind).toBe('entries');
        if (plan.kind === 'entries') expect(plan.entries.map(entry => entry.label)).toEqual(expect.arrayContaining(['require not role', 'require not claim']));
    });
    it('should explain precedence and recognize the keyword', () => {
        expect(clauseKeywords).toContain('not');
        expect(keywordDocs.require).toContain('not, and, or');
    });
    it('should validate negated roles claims and groups through the compiler', () => {
        expect(validateLines(['policy PersonOnly', '  require authenticated and not (role "Service" or claim "actorKind" matches "service")'])).toEqual([]);
    });
    it('should highlight unary not in policy conditions', async () => {
        const compilerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
        const lexerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
        const { compile } = await import(compilerPath);
        const { MonarchTokenizer } = await import(lexerPath);
        const tokenizer = new MonarchTokenizer({}, {}, 'screenplay', compile('screenplay', createTokensProvider([])), {
            getValue: () => 20000,
            onDidChangeConfiguration: () => ({ dispose() {} }),
        });
        try {
            for (const line of ['require not role "Service"', 'require not claim "kind" matches "service"', 'require not (authenticated or role "Service")']) {
                const tokens: Token[] = tokenizer.tokenize(line, true, tokenizer.getInitialState()).tokens;
                expect(tokens.filter(token => token.offset <= line.indexOf('not')).at(-1)!.type).toBe('keyword.play');
            }
        } finally {
            tokenizer.dispose();
        }
    });
    it('should report a missing operand', () => {
        expect(validateLines(['policy PersonOnly', '  require not']).map(issue => issue.code)).toContain('PLAY0116');
    });
    it('should report a negated opaque policy reference', () => {
        validateLines(['policy PersonOnly', '  require not OpaquePolicy']).map(issue => issue.code).should.deep.equal(['PLAY0115']);
    });
    it('should report an unclosed negated group', () => {
        validateLines(['policy PersonOnly', '  require not (authenticated']).map(issue => issue.code).should.deep.equal(['PLAY0117']);
    });
    it('should report a missing negated role name', () => {
        validateLines(['policy PersonOnly', '  require not role']).map(issue => issue.code).should.deep.equal(['PLAY0118']);
    });
    it('should forward every policy parse diagnostic', () => {
        const lines = ['policy PersonOnly', '  require not'];
        for (const code of ['PLAY0115', 'PLAY0116', 'PLAY0117', 'PLAY0118', 'PLAY0119', 'PLAY0120', 'PLAY0121']) {
            const diagnostics = [{ code, severity: 'error' as const, message: 'Invalid policy condition', location: { line: 2, column: 3 } }];
            validateLines(lines, { compilerDiagnostics: diagnostics }).map(issue => issue.code).should.deep.equal([code]);
        }
    });
});
