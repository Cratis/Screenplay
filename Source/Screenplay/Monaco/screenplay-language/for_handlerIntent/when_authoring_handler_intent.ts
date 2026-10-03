// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="node" />

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { completionEntriesFor, planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { createTokensProvider } from '../tokens';
import { validateLines } from '../validation';
import { responseAnalysis } from '../response-analysis';

const lines = ['command C', '  handler', '    implementation', '      hint "Keep the cutoff"', '      file C.cs'];

describe('when authoring handler implementation intent', () => {
    it('should offer wrapper only at handler and hints only at its wrapper', () => {
        expect(completionEntriesFor(['handler', 'command']).map(entry => entry.label)).toContain('implementation');
        expect(completionEntriesFor(['performer', 'query']).map(entry => entry.label)).not.toContain('implementation');
        expect(completionEntriesFor(['implementation', 'handler', 'command']).map(entry => entry.label)).toContain('hint');
        expect(completionEntriesFor(['implementation', 'command'])).toEqual([]);
    });
    it('should document contextual syntax without reserving ordinary names', () => {
        expect(hoverContent(lines, 2, 'implementation', 5, 19)).toContain('pending');
        expect(hoverContent(lines, 3, 'hint', 7, 11)).toContain('nonblank');
        expect(hoverContent(['command C', '  implementation String'], 1, 'implementation', 3, 17)).toBeNull();
    });
    it('should share parser diagnostics at original source locations', () => {
        const source = ['command C', '  handler', '    implementation', '      hint " "'];
        const issue = validateLines(source).find(issue => issue.code === 'PLAY0493');
        expect(issue?.line).toBe(3);
        expect(issue?.startColumn).toBe(7);
        expect(responseAnalysis(source).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0493')).toHaveLength(1);
    });
    it('should isolate code fences', () => {
        const source = ['command C', '  handler', '    implementation', '      ```csharp', '      hint " "', '      ```'];
        expect(validateLines(source).filter(issue => issue.code === 'PLAY0493')).toEqual([]);
        expect(planCompletions(source, 4, '      ')).toEqual({ kind: 'none' });
        expect(hoverContent(source, 4, 'hint', 7, 11)).toBeNull();
    });
    it('should keep new words out of global tokens and use contextual states in both editors', () => {
        const tokens = createTokensProvider([]);
        expect(tokens.keywords).not.toContain('implementation');
        expect(tokens.keywords).not.toContain('hint');
        expect(tokens.tokenizer.handlerBody).toBeDefined();
        expect(tokens.tokenizer.implementationBody).toBeDefined();
        const grammar = JSON.parse(readFileSync(new URL('../../../VSCodeExtension/syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
        expect(grammar.repository['handler-block'].patterns[0].include).toBe('#handler-implementation');
        expect(grammar.repository['handler-implementation'].patterns.some((pattern: { include?: string }) => pattern.include === '#fenced-csharp')).toBe(true);
    });
});
