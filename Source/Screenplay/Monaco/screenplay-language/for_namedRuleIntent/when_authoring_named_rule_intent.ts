// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';
import { createTokensProvider } from '../tokens';

const source = ['command Submit', '  label String', '  validate', '    label rule Check severity warning message "Invalid"', '      implementation', '        hint "Keep"', '        file A.cs'];
const labels = (lines: string[], index: number, indent: string) => {
    const plan = planCompletions(lines, index, indent);
    return plan.kind === 'entries' ? plan.entries.map(item => item.label) : [];
};

describe('when authoring command named-rule intent', () => {
    it('should complete only the command named-rule owner, including severity/message suffixes', () => {
        expect(labels(source, 4, '      ')).toContain('implementation');
        expect(labels(source, 5, '        ')).toEqual(['hint', 'file', 'csharp']);
        expect(labels(['concept Label : String', '  validate', '    rule Check', '      '], 3, '      ')).not.toContain('implementation');
        expect(labels(['command C', '  label String', '  validate', '    label not empty', '      '], 4, '      ')).not.toContain('implementation');
    });
    it('should apply the BMP identifier boundary to the rule name', () => {
        const lines = (name: string) => ['command C', '  label String', '  validate', `    label rule ${name}`, '      '];
        expect(labels(lines('Check'), 4, '      ')).toContain('implementation');
        expect(labels(lines('Check\u{10400}'), 4, '      ')).not.toContain('implementation');
    });
    it.each(['hint', 'implementation'])('should keep contextual property names %s and predicate names legal', word => {
        const lines = ['command C', `  ${word} String`, '  validate', `    ${word} rule RuleNameShadow severity information`, '      implementation', '        hint "Keep"'];
        expect(labels(lines, 4, '      ')).toContain('implementation');
        expect(hoverContent(lines, 1, word, 3, 3 + word.length)).toBeNull();
    });
    it('should hover only actual wrapper and hint tokens, never strings or code', () => {
        expect(hoverContent(source, 4, 'implementation', 7, 21)).toContain('PLAY0268');
        expect(hoverContent(source, 5, 'hint', 9, 13)).toContain('RuleContext');
        const lines = source.slice(0, 5).concat(['        hint "implementation"', '        ```csharp', '        implementation', '        ```']);
        expect(hoverContent(lines, 5, 'implementation', 15, 29)).toBeNull();
        expect(hoverContent(lines, 7, 'implementation', 9, 23)).toBeNull();
        expect(planCompletions(lines, 7, '        ')).toEqual({ kind: 'none' });
    });
    it('should map malformed hints to current source, not other-file coordinates', () => {
        const lines = source.map(line => line === '        hint "Keep"' ? '        hint " "' : line);
        const issue = validateLines(lines).find(issue => issue.code === 'PLAY0493');
        expect(issue?.line).toBe(5);
        expect(issue?.startColumn).toBe(9);
        const fenced = source.slice(0, 5).concat(['        ```csharp', '        hint " "', '        ```']);
        expect(validateLines(fenced).filter(issue => issue.code === 'PLAY0493')).toEqual([]);
    });
    it('should provide command-local Monaco and TextMate token states', () => {
        const tokens = createTokensProvider([]);
        expect(tokens.tokenizer.commandValidation).toBeDefined();
        expect(tokens.tokenizer.namedRuleBody).toBeDefined();
        const rulePattern = tokens.tokenizer.commandValidation[1];
        expect(Array.isArray(rulePattern) && rulePattern[0] instanceof RegExp && rulePattern[0].test('    label rule Unicodeé severity warning message "Invalid"')).toBe(true);
        expect(tokens.keywords).not.toContain('implementation');
        const grammar = JSON.parse(readFileSync(new URL('../../../VSCodeExtension/syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
        expect(grammar.repository['command-validation'].patterns[0].include).toBe('#command-named-rule');
        expect(grammar.repository['command-named-rule'].patterns[0].include).toBe('#handler-implementation');
    });
});
