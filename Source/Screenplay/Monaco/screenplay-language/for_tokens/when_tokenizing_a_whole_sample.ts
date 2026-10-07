// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { createTokensProvider } from '../tokens';

describe('when tokenizing a whole sample', () => {
    it('should tokenize every line, including a generated property without the identifier marker', async () => {
        const compilerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
        const lexerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
        const { compile } = await import(compilerPath);
        const { MonarchTokenizer } = await import(lexerPath);
        const tokenizer = new MonarchTokenizer({ getLanguageIdByLanguageName: () => null, getLanguageIdByMimeType: () => null, isRegisteredLanguageId: () => false, requestBasicLanguageFeatures() {} }, {}, 'screenplay', compile('screenplay', createTokensProvider([])), {
            getValue: () => 20000,
            onDidChangeConfiguration: () => ({ dispose() {} }),
        });
        try {
            const lines = readFileSync(new URL('../../screenplay-editor/samples/invoicing.play', import.meta.url), 'utf8').split('\n');
            expect(lines.length).toBeGreaterThan(500);
            let state = tokenizer.getInitialState();
            for (const line of lines) state = tokenizer.tokenize(line, true, state).endState;
        } finally {
            tokenizer.dispose();
        }
    });
});
