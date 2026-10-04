// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { operationCompletions, operationDetails } from '../operation-authoring';
import { responseAnalysis } from '../response-analysis';
import { hoverContent } from '../hover-content';
import { destinationHints } from '../production-destinations';
import { fileReferences } from '../file-references';
import { responseTokens } from '../response-tokens';
import { scanDocument, mergeSymbols } from '../symbols';
import { validateLines } from '../validation';

const fixture = readFileSync(new URL('../../../../../Documentation/screenplay/fixtures/operations.play', import.meta.url), 'utf8');
const lines = fixture.split('\n');

function complete(source: string, marker: string, application = mergeSymbols()) {
    const lines = source.split('\n');
    const line = lines.findIndex(line => line.includes(marker));
    return operationCompletions(lines, line, lines[line], { ...mergeSymbols(scanDocument(lines), application), authoringPath: application.authoringPath, authoringPlacement: application.authoringPlacement });
}

describe('when authoring operation intent', () => {
    it('should compile the documentation source fixture and keep typed optional collection inputs', () => {
        expect(parse(fixture).diagnostics.filter(diagnostic => diagnostic.severity === 'error')).toEqual([]);
        const analysis = responseAnalysis(lines);
        const operation = analysis.operations.declarations.find(operation => operation.name === 'SendWelcomeEmail')!;
        expect(operation.inputs.map(input => [input.name, input.type.name, input.type.isCollection, input.type.isOptional])).toEqual([
            ['recipient', 'EmailAddress', false, false], ['delivery', 'Delivery', false, false], ['tags', 'String', true, true]
        ]);
        expect(operationDetails(operation)).toContain('execute: pending');
        expect(operationDetails(operation)).toContain('Use the existing mail adapter');
        expect(fileReferences(lines).map(reference => reference.path)).toEqual(['Adapters/OpenCostCenter.cs']);
    });
    it('should complete mapping sources from the current typed command rather than another file’s line positions', () => {
        const other = scanDocument(['command Other', '  foreign String']);
        const source = fixture.replace('recipient EmailAddress = owner.email', 'recipient EmailAddress = ow');
        const entries = complete(source, 'recipient EmailAddress = ow', other)!;
        expect(entries.map(entry => entry.label)).toEqual(['projectId', 'name', 'owner', 'delivery', 'tags']);
        expect(entries.some(entry => entry.label === 'foreign')).toBe(false);
        expect(entries.find(entry => entry.label === 'owner')?.documentation).toContain('Owner');
        expect(complete(fixture.replace('recipient EmailAddress = owner.email', 'recipient EmailAddress = owner.'), 'recipient EmailAddress = owner.')?.map(entry => entry.label)).toEqual(['email', 'name']);
    });
    it('should resolve placed and imported scoped declarations and retain their real source locations', () => {
        const other = { path: 'intent.play', source: 'system Mailer\nslice StateChange Shared\n  operation Send\n    uses Mailer\n    recipient String optional\n    execute\n      implementation\n        hint "Use mail"', placement: ['M', 'F'] };
        const current = 'slice StateChange Here\n  command C\n    local String\n    produces Shared.Se\n';
        const application = { ...mergeSymbols(scanDocument(other.source.split('\n'))), authoringDocuments: [other], authoringPath: 'current.play', authoringPlacement: ['M', 'F'] };
        const entries = complete(current, 'produces Shared.Se', application)!;
        expect(entries.find(entry => entry.label === 'Send')?.documentation).toContain('M.F.Shared.Send');
        const operation = responseAnalysis(current.split('\n'), [other], ['M', 'F']).operations.declarations[0];
        expect(operation.location).toEqual({ path: 'intent.play', line: 3, column: 3 });
    });
    it('should avoid guessing a kind or offering an ambiguous short name', () => {
        const source = 'system Mailer\nmodule M\n  feature F\n    slice StateChange A\n      operation Send\n        uses Mailer\n    slice StateChange B\n      event Send\n    slice StateChange C\n      command Ask\n        produces Se\n';
        const entries = complete(source, 'produces Se')!;
        expect(entries.map(entry => entry.label)).toContain('M.F.A.Send');
        expect(entries.find(entry => entry.label === 'Send')?.documentation).toBe('Declared event.');
        expect(destinationHints(source.replace('produces Se', 'produces Send').split('\n'))).toEqual([]);
    });
    it('should expose exact typed input spans and source hover without converting keyword properties into nodes', () => {
        const source = 'system Mailer\ncommand C\n  text String\n  produces operation Send\n    uses Mailer\n    command String = text\n    execute String = text\n    @uses String = text\n    execute\n      implementation\n        hint "Guide"\n';
        const lines = source.split('\n');
        const operation = responseAnalysis(lines).operations.declarations[0];
        expect(operation.inputs.map(input => input.name)).toEqual(['command', 'execute', 'uses']);
        expect(hoverContent(lines, 5, 'command', 5, 12)).toContain('Operation input');
        expect(hoverContent(lines, 5, 'text', 22, 26)).toContain('Command source');
        const tokens = responseTokens(lines);
        expect(tokens.find(token => token.line === 5 && token.column === 4)).toMatchObject({ type: 1, length: 7 });
        expect(tokens.some(token => token.line === 6 && token.type === 0)).toBe(false);
        expect(tokens.some(token => token.line === 8 && token.type === 0)).toBe(true);
        expect(hoverContent(lines, 10, 'Guide', 15, 20)).toBeNull();
    });
    it('should distinguish input and source hover even when their names are identical', () => {
        const lines = ['system Mailer', 'command C', '  input String', '  produces operation Send', '    uses Mailer', '    input String = input'];
        const column = lines[5].lastIndexOf('input') + 1;
        expect(hoverContent(lines, 5, 'input', 5, 10)).toContain('Operation input');
        expect(hoverContent(lines, 5, 'input', column, column + 5)).toContain('Command source');
    });
    it('should complete qualified partial assertions with typed enum values', () => {
        const source = 'system Mailer\nconcept Delivery : Enum\n  immediate\n  digest\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n        mode Delivery optional\n      command C\n        produces Send\n      specification T\n        when C\n        then operation S.Send\n          mode = ';
        expect(complete(source, 'mode = ')?.map(entry => entry.insertText)).toEqual(['"immediate"', '"digest"']);
    });
    it('should complete conditional production targets without inserting synthetic scope names', () => {
        const source = 'system Mailer\noperation Send\n  uses Mailer\ncommand C\n  accepted Bool\n  produces when accepted == true\n    Se';
        const entries = complete(source, '    Se')!;
        expect(entries.map(entry => entry.insertText)).toEqual(['Send']);
        expect(entries[0].documentation).not.toContain('EditorAuthoring');
    });
    it('should recognize isolated reaction fragments and reject operation productions without event advice', () => {
        for (const production of ['produces operation Send\n      uses Mailer', 'produces Send']) {
            const source = 'reaction R\n  every 1 day\n    ' + production;
            const application = { ...mergeSymbols(), authoringDocuments: [{ path: 'operation.play', source: 'system Mailer\noperation Send\n  uses Mailer' }] };
            const issues = validateLines(source.split('\n'), { application });
            expect(issues.map(issue => issue.code)).toContain('PLAY0499');
            expect(issues.some(issue => issue.code === 'PLAY0166')).toBe(false);
        }
    });
    it('should not apply event metadata diagnostics to typed inline operation inputs', () => {
        const source = 'system Mailer\ncommand C\n  count Int\n  produces operation Send\n    uses Mailer\n    sequence Int = count\n    namespace Int = count\n    occurred Int = count\n';
        expect(validateLines(source.split('\n')).filter(issue => issue.code === 'PLAY0476')).toEqual([]);
        expect(validateLines(['command C', '  produces event Recorded', '    sequence Int = 1']).map(issue => issue.code)).toContain('PLAY0476');
    });
    it('should suppress cross-file operation destination hints in assembled context but preserve certain legacy events', () => {
        const current = ['module M', '  feature F', '    slice StateChange Here', '      command C', '        produces Shared.Send', '        produces Recorded'];
        const other = { path: 'other.play', source: 'system Mailer\nmodule M\n  feature F\n    slice StateChange Shared\n      operation Send\n        uses Mailer\n      event Recorded' };
        const application = { ...mergeSymbols(scanDocument(other.source.split('\n'))), authoringDocuments: [other] };
        expect(responseAnalysis(current, [other]).operations.references.map(reference => [reference.location.line - 1, reference.kind])).toEqual([[4, 'operation'], [5, 'event']]);
        expect(scanDocument(current).commands[0].produces?.map(production => production.name)).toEqual(['Recorded']);
        expect(destinationHints(current, application).map(hint => hint.label)).toEqual(['for <new event source>']);
        expect(destinationHints(['command C', '  produces Missing'])).toEqual([]);
    });
    it('should keep comments and fenced source out of operation completions', () => {
        expect(complete('system Mailer\noperation Send\n  uses Mailer\n  execute\n    ```csharp\n    produces Other\n    ```', 'produces Other')).toBeNull();
        expect(complete('system Mailer\noperation Send\n  uses Mailer // Note', '// Note')).toBeNull();
    });
});
