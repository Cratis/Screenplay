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
    it.each([0, 2])('should own source completions from the unsaved current document with foreign offset %s', offset => {
        const current = ['command Current', '  mine String', '  returns '];
        const foreign = [...Array<string>(offset).fill(''), 'command Other', '  foreign String'];
        const symbols = mergeSymbols(scanDocument(current), scanDocument(foreign));
        expect(responseCompletions(current, 2, current[2], symbols)?.map(entry => entry.insertText)).toEqual(['@mine']);
        const block = ['command Current', '  mine String', '  returns', '    result = '];
        expect(responseCompletions(block, 3, block[3], mergeSymbols(scanDocument(block), scanDocument(foreign)))?.map(entry => entry.label)).toEqual(['mine']);
    });
    it('should retain placement and source identities while merging sibling declarations', () => {
        const current = ['slice StateChange S', '  command C', '    id Id generated identifier', '    returns @missing'];
        const siblings = [{ path: 'types.play', source: 'concept Id : Uuid' }, { path: 'other.play', source: 'slice StateChange Other\n  command C\n    foreign String', placement: ['M', 'Other'] }];
        const alone = responseAnalysis(current, [], ['M', 'F'], 'slice.play');
        const merged = responseAnalysis(current, siblings, ['M', 'F'], 'slice.play');
        expect([...merged.commands.values()].map(command => command.name)).toEqual(['C']);
        expect(merged.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0487')).toEqual(alone.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0487'));
        expect(merged.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0483')).toEqual([]);
        expect(merged.commands.get(1)?.properties[0].name).toBe('id');
        expect(responseAnalysis(current.map(line => line.replace('@missing', '@id')), siblings, ['M', 'F'], 'slice.play').diagnostics).toEqual([]);
    });
    it('should show the complete explicit declared type without admitting collection responses', () => {
        const source = ['command C', '  values String[] optional', '  returns', '    result String[] optional = values'];
        expect(hoverContent(source, 3, 'result', 5, 11)).toContain('String[] optional (explicit)');
        expect(validateLines(source).filter(issue => issue.code === 'PLAY0489')).toHaveLength(1);
    });
    it.each([['result', 'id'], ['résultat', 'idé'], ['@result', '@id']])('should hover only actual response tokens for %s and %s', (field, sourceName) => {
        const source = ['command C', `  ${sourceName} String`, '  returns', `    ${field} = ${sourceName} // ${field} ${sourceName}`];
        const hoverAt = (token: string, column: number) => hoverContent(source, 3, token.replace('@', ''), column + (token.startsWith('@') ? 1 : 0), column + token.length);
        expect(hoverAt(field, 5)).toContain('(inferred)');
        const valueColumn = source[3].indexOf('= ') + 3;
        expect(hoverAt(sourceName, valueColumn)).toContain('(inferred)');
        expect(hoverAt(field, source[3].lastIndexOf(field) + 1)).toBeNull();
        expect(hoverAt(sourceName, source[3].lastIndexOf(sourceName) + 1)).toBeNull();
    });
    it('should respect quoted and escaped comment delimiters without hovering string occurrences', () => {
        const source = ['command C', '  id String', '  returns', '    result = id // "result"', '  description', '    "escaped \\" // result"', '    ```text', '    result = id', '    ```'];
        expect(hoverContent(source, 3, 'result', 5, 11)).toContain('(inferred)');
        expect(hoverContent(source, 3, 'result', 21, 27)).toBeNull();
        expect(hoverContent(source, 5, 'result', 21, 27)).toBeNull();
        expect(hoverContent(source, 7, 'result', 5, 11)).toBeNull();
    });
    it('should ignore comments and fences and keep keyword-shaped input mappings', () => {
        const source = ['command C', '  generated String // request name', '  @returns String', '  description', '    ```text', '    returns', '      fake = generated', '    ```'];
        expect(scanDocument(source).commands[0].properties.map(property => property.name)).toEqual(['generated', 'returns']);
        expect(scanDocument(source).commands[0].response).toBeNull();
        expect(labels(source, '  // returns ')).toEqual([]);
        expect(hoverContent(source, 5, 'returns', 5, 12)).toBeNull();
    });
});
