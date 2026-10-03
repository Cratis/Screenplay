// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="node" />

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { hoverContent } from '../hover-content';
import { planCompletions } from '../completion-planner';
import { responseTokens } from '../response-tokens';
import { responseAnalysis } from '../response-analysis';
import { responseCompletions } from '../response-completions';
import { mergeSymbols, scanDocument } from '../symbols';
import { validateLines } from '../validation';

const fixture = readFileSync(new URL('../../../../../Documentation/screenplay/fixtures/generated-responses.play', import.meta.url), 'utf8');
const lines = fixture.trimEnd().split('\n');
const labels = (source: string[], before: string) => {
    const plan = planCompletions([...source, before], source.length, before);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
};

describe('when authoring syntax-only responses', () => {
    it('should parse the complete documentation fixture without syntax errors', () => expect(parse(fixture).diagnostics).toEqual([]));
    it('should index generated properties but not response fields as request properties', () => {
        const command = scanDocument(lines).commands[0];
        expect(command.properties.map(property => property.name)).toEqual(['projectId', 'receiptId', 'name']);
        expect(command.properties.filter(property => property.isGenerated).map(property => property.name)).toEqual(['projectId', 'receiptId']);
        expect(command.response?.kind).toBe('RecordCommandResponseSyntax');
    });
    it('should share one typed analysis and symbol index across repeated revision reads', () => {
        expect(responseAnalysis([...lines])).toBe(responseAnalysis(lines));
        expect(scanDocument([...lines])).toBe(scanDocument(lines));
    });
    it('should resolve scalar highlighting through typed syntax while keeping keyword-shaped names', () => {
        const source = ['concept lower : String', 'command C', '  returns lower', '  lower String'];
        expect(responseTokens(source).some(token => token.line === 2 && token.column === 2 && token.type === 0)).toBe(true);
        expect(responseTokens(['command C', '  returns lower'])).toEqual([]);
        expect(responseTokens(['command C', '  generated String'])).toEqual([]);
    });
    it('should not offer generated form fields', () => {
        const source = ['concept Id : Uuid', 'command C', '  id Id generated identifier', '  name String', 'form F for C'];
        expect(labels(source, '  field ')).toEqual(['name']);
    });
    it('should disclose unavailable execution without treating valid syntax as invalid', () => {
        const issues = validateLines(lines);
        expect(issues.every(issue => issue.severity === 'information' && issue.code === 'PLAY0268')).toBe(true);
        expect(issues.length).toBeGreaterThan(4);
    });
    it('should show inferred response types and generated-not-input status', () => {
        const line = lines.findIndex(line => line.trim() === 'projectId = projectId');
        expect(hoverContent(lines, line, 'projectId', 11, 20)).toContain('ProjectId (inferred)');
        expect(hoverContent(lines, line, 'projectId', 11, 20)).toContain('not request input');
        expect(hoverContent(lines, line, 'projectId', 11, 20)).toContain('unavailable');
    });
    it('should preserve local returns ambiguity and escaped names in reordered declarations', () => {
        const source = ['concept lower : String', 'command C', '  returns lower', '  generated String', '  @returns String', '  lower lower'];
        const command = scanDocument(source).commands[0];
        expect(command.response?.kind).toBe('ScalarCommandResponseSyntax');
        expect(command.properties.map(property => property.name)).toEqual(['generated', 'returns', 'lower']);
        expect(scanDocument(['command C', '  returns lower']).commands[0].response).toBeNull();
        expect(validateLines(['command C', '  returns @missing']).map(issue => issue.code)).toContain('PLAY0487');
    });
    it('should offer sources and eligible generated modifiers contextually', () => {
        const source = ['concept Id : Uuid', 'command C', '  id Id generated identifier', '  receipt Id generated', '  name String'];
        expect(labels(source, '  returns ')).toEqual(['id', 'receipt', 'name']);
        expect(labels(source, '  other Id ')).toContain('generated');
        expect(labels(source, '  other Uuid ')).not.toContain('generated');
        expect(labels([...source, '  returns'], '    ')).toEqual(['id', 'receipt', 'name']);
        expect(labels([...source, '  returns'], '    result = ')).toEqual(['id', 'receipt', 'name']);
        expect(labels([...source, 'specification S', '  when C'], '    ')).toEqual(['name', 'generated receipt', 'for']);
        expect(labels([...source, 'specification S', '  when C'], '    generated ')).toEqual(['receipt']);
    });
    it('should offer response assertion fields, not source property names', () => {
        const source = ['command C', '  value String', '  returns', '    result = value', 'specification S', '  when C', '    value = "test"', '  then returns'];
        expect(labels(source, '    ')).toEqual(['result']);
    });
    it('should use cross-file and unsaved concept declarations without inventing regex diagnostics', () => {
        const source = ['command C', '  id Imported generated', '  returns @id'];
        const application = scanDocument(['concept Imported : Uuid']);
        expect(validateLines(source, { application }).filter(issue => issue.severity !== 'information')).toEqual([]);
        const current = [...source, '  other Imported '];
        expect(responseCompletions(current, 3, current[3], mergeSymbols(scanDocument(current), application))?.map(entry => entry.label)).toContain('generated');
        expect(validateLines(source, { application: scanDocument(['concept Imported : String']) }).map(issue => issue.code)).toContain('PLAY0483');
    });
    it('should ignore comments and fences and keep keyword-shaped input mappings', () => {
        const source = ['command C', '  generated String // request name', '  @returns String', '  description', '    ```text', '    returns', '      fake = generated', '    ```'];
        expect(scanDocument(source).commands[0].properties.map(property => property.name)).toEqual(['generated', 'returns']);
        expect(scanDocument(source).commands[0].response).toBeNull();
        expect(labels(source, '  // returns ')).toEqual([]);
        expect(hoverContent(source, 5, 'returns', 5, 12)).toBeNull();
    });
});
