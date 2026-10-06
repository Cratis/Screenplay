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

describe('when authoring ESM v7 responses', () => {
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
    it.each(['s', 'return', 'turn', 'returns', 'name', '@s', '@return', '@turn', '@returns', '@name'])('should hover and tokenize scalar operand %s without overlapping the keyword', operand => {
        const property = operand.replace('@', '');
        for (const separator of [' ', ' \u2003\t']) {
            const source = ['command C', `  ${operand} String`, `  returns${separator}${operand} // ${operand}`];
            const column = 9 + separator.length + (operand.startsWith('@') ? 1 : 0);
            expect(responseTokens(source).filter(token => token.line === 2)).toEqual([
                { line: 2, column: 2, length: 7, type: 0 },
                { line: 2, column, length: property.length, type: 1 },
            ]);
            expect(hoverContent(source, 2, 'returns', 3, 10)).toContain('String');
            expect(hoverContent(source, 2, property, column + 1, column + 1 + property.length)).toContain('String');
            const commentColumn = source[2].lastIndexOf(operand) + 1 + (operand.startsWith('@') ? 1 : 0);
            expect(hoverContent(source, 2, property, commentColumn, commentColumn + property.length)).toBeNull();
        }
    });
    it('should not offer generated form fields', () => {
        const source = ['concept Id : Uuid', 'command C', '  id Id generated identifier', '  name String', 'form F for C'];
        expect(labels(source, '  field ')).toEqual(['name']);
    });
    it('should not report unadmitted execution for ESM v7 responses and generated values', () => {
        expect(validateLines(lines)).toEqual([]);
    });
    it('should show inferred response types and generated-not-input status', () => {
        const line = lines.findIndex(line => line.trim() === 'projectId = projectId');
        expect(hoverContent(lines, line, 'projectId', 11, 20)).toContain('ProjectId (inferred)');
        expect(hoverContent(lines, line, 'projectId', 11, 20)).toContain('not request input');
        expect(hoverContent(lines, line, 'projectId', 11, 20)).toContain('Executable as ESM v7');
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
        const modifiers = responseCompletions([...source, '  other Id '], source.length, '  other Id ', scanDocument(source));
        expect(modifiers?.find(entry => entry.label === 'generated')?.documentation).toContain('no validation rules');
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
    it.each([
        ['"01234567-89ab-cdef-0123-456789abcdef"', []],
        ['42', ['PLAY0490']],
    ])('should validate an isolated specification against all sibling fragments for %s', (value, codes) => {
        const current = ['specification S', '  when C', `    for ${value}`];
        const other = [{ path: 'types.play', source: 'concept Id : Uuid' }, { path: 'command.play', source: 'command C\n  id Id generated identifier' }];
        const analysis = responseAnalysis(current, other, undefined, 'spec.play');
        expect(analysis.diagnostics.map(diagnostic => diagnostic.code)).toEqual(codes);
        expect(analysis.specifications.get(0)?.location).toEqual({ path: 'spec.play', line: 1, column: 1 });
        expect(analysis.diagnostics.every(diagnostic => diagnostic.location.path === 'spec.play' && diagnostic.location.line === 3)).toBe(true);
        expect(validateLines(current, { application: mergeSymbols(...other.map(document => scanDocument(document.source.split('\n')))), path: 'spec.play' }).filter(issue => issue.severity !== 'information').map(issue => issue.code)).toEqual(codes);
    });
    it('should retain split generated fixtures and response references at equal source line numbers', () => {
        const command = 'command C\n  receipt Id generated\n  returns\n    result = receipt';
        const other = [{ path: 'types.play', source: 'concept Id : Uuid' }, { path: 'command.play', source: command }, { path: 'unrelated.play', source: 'command Other\n  value String' }];
        const source = ['specification S', '  when C', '    generated receipt = 42', '  then returns', '    result = 42'];
        expect(responseAnalysis(source, other, undefined, 'spec.play').diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0490', 'PLAY0491']);
        const valid = source.map(line => line.replace('42', '"01234567-89ab-cdef-0123-456789abcdef"'));
        expect(responseAnalysis(valid, other, undefined, 'spec.play').diagnostics).toEqual([]);
        const invalidCommand = command.replace('result = receipt', 'result = missing').split('\n');
        const analysis = responseAnalysis(invalidCommand, [other[0], { path: 'spec.play', source: valid.join('\n') }, other[2]], undefined, 'command.play');
        expect([...analysis.commands.values()].map(command => command.name)).toEqual(['C']);
        expect(analysis.commands.get(0)?.response?.location.path).toBe('command.play');
        expect(analysis.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0487']);
        expect(analysis.diagnostics[0].location).toEqual({ path: 'command.play', line: 4, column: 14 });
    });
    it('should refuse ambiguous sibling commands rather than inventing a fixture relationship', () => {
        const source = ['specification S', '  when C', '    generated value = 42'];
        const other = ['concept Id : Uuid', 'command C\n  value Id generated', 'command C\n  value String'];
        expect(responseAnalysis(source, other).diagnostics).toEqual([]);
        const symbols = mergeSymbols(scanDocument(source), ...other.map(source => scanDocument(source.split('\n'))));
        expect(responseCompletions([...source, '    generated '], 3, '    generated ', symbols)).toBeNull();
    });
    it('should leave real duplicate declarations and explicit placement checks to the compiler', () => {
        const source = ['slice StateChange S', '  command C', '    id Id generated'];
        const sibling = ['slice StateChange S', '  command Other'];
        const other = ['concept Id : Uuid', sibling.join('\n')];
        expect(responseAnalysis(source, other).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0173');
        expect(validateLines(source, { application: mergeSymbols(...other.map(source => scanDocument(source.split('\n')))) }).map(issue => issue.code)).toContain('PLAY0173');
        const placed = ['command C', '  id Id generated'];
        expect(responseAnalysis(placed, ['concept Id : Uuid'], ['M', 'F'], 'placed.play').diagnostics.length).toBeGreaterThan(0);
        expect([...responseAnalysis(placed, ['concept Id : Uuid'], ['M', 'F'], 'placed.play').commands.values()]).toEqual([]);
    });
    it('should honor explicit scopes rather than tying equally named placed commands to fragments', () => {
        const source = ['slice StateChange S', '  specification Check', '    when C', '      for 42', '    then returns 42'];
        const other = [
            { path: 'types.play', source: 'concept Id : Uuid' },
            { path: 'generated.play', source: 'slice StateChange Generated\n  command C\n    id Id generated identifier\n    returns @id', placement: ['M', 'F1'] },
            { path: 'local.play', source: 'slice StateChange Local\n  command C\n    id Int identifier\n    returns @id', placement: ['M', 'F2'] },
        ];
        expect(responseAnalysis(source, other, ['M', 'F2'], 'spec.play').diagnostics).toEqual([]);
        expect(responseAnalysis(source, other, ['M', 'F1'], 'spec.play').diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0490', 'PLAY0491']);
    });
    it('should not collide with real declarations using synthetic wrapper names', () => {
        const source = ['specification S', '  when C', '    for 42'];
        const real = 'module EditorAuthoring\n  feature FragmentFeature\n    slice StateChange Fragment0\n      command Other\n        value String';
        expect(responseAnalysis(source, ['concept Id : Uuid', 'command C\n  id Id generated identifier', real]).diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0490']);
    });
    it.each(['command', '@command'])('should own responses with a keyword-shaped property %s', name => {
        const source = ['command C', `  ${name} String`, '  returns @command'];
        expect(scanDocument(source).commands.map(command => command.name)).toEqual(['C']);
        expect(labels(source.slice(0, 2), '  returns @')).toEqual(['command']);
        expect(hoverContent(source, 2, 'returns', 3, 10)).toContain('String');
        expect(hoverContent(source, 2, 'command', 12, 19)).toContain('String');
        const record = ['command C', `  ${name} String`, '  returns', '    @result = @command'];
        expect(labels(record.slice(0, 3), '    result = ')).toEqual(['command']);
        expect(hoverContent(record, 3, 'result', 6, 12)).toContain('String (inferred)');
    });
    it.each([
        ['String optional', 'String optional'],
        ['String[]', 'String[]'],
        ['String[] optional', 'String[] optional'],
        ['String?', 'String optional'],
    ])('should render complete response types in assertion completions and hover for %s', (declared, rendered) => {
        for (const explicit of [false, true]) {
            const source = ['command C', `  value ${declared}`, '  returns', `    result ${explicit ? `${declared} ` : ''}= value`, 'specification S', '  when C', '  then returns', '    '];
            const completion = responseCompletions(source, 7, source[7], scanDocument(source));
            expect(completion?.[0].documentation).toBe(`${rendered}. Executable as ESM v7. Generated values require fixtures in reference execution; other unadmitted constructs still prevent binding.`);
            expect(hoverContent(source, 3, 'result', 5, 11)).toContain(`${rendered} (${explicit ? 'explicit' : 'inferred'})`);
        }
    });
    it('should combine ordinary optional and required generation suggestions without invalid combinations', () => {
        const source = ['concept Id : Uuid', 'command C'];
        expect(labels(source, '  id Id ')).toEqual(['optional', 'generated', 'generated identifier', 'identifier']);
        expect(labels(source, '  id Id o')).toEqual(['optional']);
        expect(labels(source, '  id Id generated ')).toEqual(['identifier']);
        for (const modifier of ['optional ', 'optional identifier ', 'identifier ', 'generated identifier ']) {
            const suggestions = labels(source, `  id Id ${modifier}`);
            expect(suggestions).not.toContain('optional');
            expect(suggestions).not.toContain('generated');
        }
        expect(validateLines([...source, '  id Id optional']).filter(issue => issue.severity !== 'information')).toEqual([]);
    });
    it('should ignore comments and fences and keep keyword-shaped input mappings', () => {
        const source = ['command C', '  generated String // request name', '  @returns String', '  description', '    ```text', '    returns', '      fake = generated', '    ```'];
        expect(scanDocument(source).commands[0].properties.map(property => property.name)).toEqual(['generated', 'returns']);
        expect(scanDocument(source).commands[0].response).toBeNull();
        expect(labels(source, '  // returns ')).toEqual([]);
        expect(hoverContent(source, 5, 'returns', 5, 12)).toBeNull();
    });
});
