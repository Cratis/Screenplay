// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
// Monaco ships its internal runtime without declarations. Exercise the shipped compiler and lexer,
// not the input regexes (Monarch recompiles them with its own flags).
// @ts-expect-error Monaco internal module has no declarations.
import { compile } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
// @ts-expect-error Monaco internal module has no declarations.
import { MonarchTokenizer } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
import { createTokensProvider } from '../tokens';

type Line = { tokens: { offset: number; type: string }[]; endState: { stack: { state: string } } };
function tokenize(lines: string[]): Line[] {
    const tokenizer = new MonarchTokenizer(
        { isRegisteredLanguageId: () => false, getLanguageIdByLanguageName: () => null, getLanguageIdByMimeType: () => null },
        {}, 'screenplay', compile('screenplay', createTokensProvider([])),
        { getValue: () => 100000, onDidChangeConfiguration: () => ({ dispose() {} }) },
    );
    try {
        let state = tokenizer.getInitialState();
        return lines.map(line => {
            const result = tokenizer.tokenize(line, true, state) as Line;
            state = result.endState;
            return result;
        });
    } finally {
        tokenizer.dispose();
    }
}
const typeAt = (line: Line, column: number) => line.tokens.filter(token => token.offset <= column).at(-1)?.type;

const body = ['  label String', '  validate', '    label rule Check', '      implementation', '        hint "Keep"', '        file A.cs'];
describe('when Monarch compiles named-rule states', () => {
    it.each(['C', 'Unicodeé', 'C\u0301\u203f\u0661'])('should enter command and wrapper states for %s', name => {
        const lines = tokenize([`command ${name}`, ...body]);
        expect(lines[0].endState.stack.state).toBe('commandBody.');
        expect(lines[3].endState.stack.state).toBe('namedRuleBody.    ');
        expect(lines[4].endState.stack.state).toBe('implementationBody.      ');
        expect(typeAt(lines[4], 6)).toBe('keyword.play');
        expect(typeAt(lines[5], 8)).toBe('keyword.play');
    });
    it.each(['éC', 'C\u0903', 'C\u2160', 'C𐐀', 'C𝟙'])('should not broaden the compiler command identifier contract for %s', name => {
        expect(tokenize([`command ${name}`])[0].endState.stack.state).toBe('root');
    });
    it('should recognize BMP property paths and rule names', () => {
        const lines = tokenize(['command C', '  validate', '    labél.part\u0301 rule Checké\u203f\u0661', '      implementation', '        hint "Keep"']);
        expect(lines[2].endState.stack.state).toBe('namedRuleBody.    ');
        expect(typeAt(lines[4], 8)).toBe('keyword.play');
    });
    it('should keep contextual properties, concepts, sibling rules and dedented commands out of wrapper states', () => {
        const lines = tokenize(['command C', '  hint String', '  implementation String', '  validate', '    label rule Check', '      implementation', '        hint "Keep"', '    label not empty', '      implementation', '        hint "Other"', 'command Next', '  implementation', '    hint "Other"', 'concept Label : String', '  validate', '    rule Check', '      implementation', '        hint "Other"']);
        for (const [line, column] of [[1, 2], [2, 2], [8, 6], [9, 8], [11, 2], [12, 4], [16, 6], [17, 8]]) {
            expect(typeAt(lines[line], column)).toBe('identifier.play');
        }
    });
    it('should preserve legacy delimiters, operators, metadata, strings, comments and embedded fences', () => {
        const lines = tokenize(['event Done', '  id "old" // pin', '  tags String[] = ["x", "y"]', 'command C', ...body.slice(0, 5), '        ```csharp', '        implementation', '        hint "code"', '        ```', '        hint "After"']);
        expect(typeAt(lines[1], 2)).toBe('keyword.play');
        expect(typeAt(lines[1], 5)).toBe('string.play');
        expect(typeAt(lines[1], 11)).toBe('comment.play');
        expect(typeAt(lines[2], 13)).toBe('delimiter.play');
        expect(typeAt(lines[2], 16)).toBe('operator.play');
        expect(typeAt(lines[10], 8)).not.toBe('identifier.play');
        expect(typeAt(lines[11], 8)).not.toBe('keyword.play');
        expect(typeAt(lines[13], 8)).toBe('keyword.play');
    });
});
