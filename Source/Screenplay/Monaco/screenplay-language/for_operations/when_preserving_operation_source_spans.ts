// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse, AuthoringProductionResolver } from '@cratis/screenplay-compiler';
import { responseAnalysis } from '../response-analysis';
import { responseTokens } from '../response-tokens';
import { operationCompletions, operationHover, operationReferenceAt } from '../operation-authoring';
import { scanDocument } from '../symbols';

const fragment = ['system Mailer', 'operation Send', '  uses Mailer'];
const vectors = JSON.parse(readFileSync(new URL('./conditional-mapping-vectors.json', import.meta.url), 'utf8'));

describe('when preserving operation source spans', () => {
    it('should remap standalone location members and emit only valid token ranges', () => {
        const operation = responseAnalysis(fragment).operations.declarations[0];
        expect(operation.usesLocation).toEqual({ path: 'current.play', line: 3, column: 8 });
        expect(operationHover(fragment, 2, 8, 14)).toContain('system Mailer');
        expect(responseTokens(fragment)).toContainEqual({ line: 2, column: 2, length: 4, type: 0 });
        for (const lines of [fragment, ['system Mailer', 'operation Send'], ['command C', '  produces operation'], [...fragment, '  execute', '    implementation', '      hint', '        ```markdown', '        Mail safely // content', '        ```', '      file Send.cs']]) {
            for (const token of responseTokens(lines)) {
                expect(token.line).toBeGreaterThanOrEqual(0);
                expect(token.column).toBeGreaterThanOrEqual(0);
                expect(token.column + token.length).toBeLessThanOrEqual(lines[token.line].length);
            }
        }
    });
    it('should shift each shared location identity once and preserve another document and real placement', () => {
        const source = [...fragment, '  execute', '    implementation', '      hint "First"', '      hint "Second\\nline"', '      ```csharp', '      // Send', '      ```'];
        const other = { path: 'intent.play', source: source.join('\n') };
        const operations = responseAnalysis(['command C', '  produces Send'], [other]).operations;
        expect(operations.declarations[0].usesLocation).toEqual({ path: 'intent.play', line: 3, column: 8 });
        expect(operations.declarations[0].execute?.implementation?.hints.map(hint => hint.location.line)).toEqual([6, 7]);
        expect(operations.declarations[0].execute?.implementation?.hints[1].text).toBe('Second\nline');
        expect(operations.declarations[0].execute?.code?.location.line).toBe(8);
        expect(operations.references[0].declaration).toBe(operations.declarations[0]);
        const placed = { path: 'placed.play', source: 'system Mailer\nslice StateChange Shared\n  operation Send\n    uses Mailer', placement: ['M', 'F'] };
        const declaration = responseAnalysis(['slice StateChange Here', '  command C', '    produces Shared.Send'], [placed], ['M', 'F'], 'current.slice.play').operations.declarations[0];
        expect(declaration.usesLocation).toEqual({ path: 'placed.play', line: 4, column: 10 });
    });
    it('should retain typed production ownership across comment-only lines of any indentation', () => {
        for (const comment of ['// boundary', '  // boundary', '    // boundary', '        // boundary']) {
            const lines = [...fragment, '  recipient String', 'command C', '  text String', '  produces Send', comment, '    recipient = text'];
            const symbols = scanDocument(lines);
            expect(operationCompletions(lines, 8, '    recipient = te', symbols)?.map(entry => entry.label)).toEqual(['text']);
            expect(operationHover(lines, 8, 17, 21, symbols)).toContain('Command source');
            expect(responseAnalysis(lines).operations.references[0].mappings[0].source.location).toEqual({ path: 'current.play', line: 9, column: 17 });
        }
    });
    it('should use typed conditional ownership for recipients and nested optional source paths', () => {
        for (const target of vectors.targets as string[]) for (const conditional of [false, true]) for (const mapping of vectors.mappings as { text: string; labels: string[] }[]) {
            const intent = { path: 'intent.slice.play', source: vectors.intent.replace('TARGET', target.split('.').at(-1)), placement: ['M', 'F'] };
            const header = conditional ? `    produces when text == "yes"\n      ${target} // target` : `    produces ${target} // target`;
            const before = `${conditional ? '        ' : '      '}${mapping.text}`;
            const lines = `${vectors.command}${header}\n// comment does not end ownership\n${before}`.split('\n');
            const symbols = { ...scanDocument(lines), authoringDocuments: [intent], authoringPath: 'current.slice.play', authoringPlacement: ['M', 'F'] };
            const entries = operationCompletions(lines, lines.length - 1, before, symbols);
            expect(entries?.map(entry => entry.label), `${target}: ${conditional}: ${mapping.text}`).toEqual(mapping.labels);
            expect(entries?.some(entry => entry.documentation?.includes('not admitted by any supported executable model (ESM) version yet (PLAY0268) (#301)'))).toBe(true);
            expect(entries?.some(entry => entry.documentation?.includes('event'))).toBe(false);
            const reference = responseAnalysis(lines, [intent], ['M', 'F'], 'current.slice.play').operations.references[0];
            expect(reference.declaration?.location.path).toBe('intent.slice.play');
            expect(reference.targetLocation?.line).toBe(conditional ? 6 : 5);
        }
    });
    it('should bind conditional, qualified and inline targets only at their parser-owned spans', () => {
        for (const target of ['Send', 'S.Send']) {
            const lines = ['system Mailer', 'module M', '  feature F', '    slice StateChange S', '      operation Send', '        uses Mailer', '      command C', '        accepted Bool', '        produces when accepted == true', `          ${target} // Send`];
            const analysis = responseAnalysis(lines).operations;
            expect(analysis.references[0].location.line).toBe(9);
            expect(analysis.references[0].targetLocation).toEqual({ path: 'current.play', line: 10, column: 11 });
            const column = 11 + target.length - 4;
            expect(operationHover(lines, 9, column, column + 4)).toContain('operation M.F.S.Send');
            expect(operationReferenceAt(analysis, lines, 9, lines[9].lastIndexOf('Send') + 1, lines[9].length + 1)).toBeUndefined();
        }
        const lines = ['system Mailer', 'command C', '  text String', '  produces operation Send // Send', '    uses Mailer', '    recipient String = text'];
        expect(responseAnalysis(lines).operations.references[0].targetLocation?.column).toBe(22);
        expect(operationHover(lines, 3, 22, 26)).toContain('operation Send');
        expect(operationHover(lines, 3, 30, 34)).toBeNull();
        expect(responseAnalysis(lines).operations.references[0].mappings[0].source.location.column).toBe(24);
    });
    it('should associate historical event shapes but retain duplicate-generation and kind collision evidence', () => {
        const prefix = 'system Mailer\nmodule M\n  feature F\n    slice StateChange S\n';
        for (const generations of ['      event Recorded\n      event Recorded generation 2\n', '      event Recorded generation 2\n      event Recorded\n']) {
            const application = parse(prefix + generations + '      command C\n        produces Recorded\n').value;
            const resolver = new AuthoringProductionResolver(application);
            const resolution = resolver.resolve('Recorded', application.modules[0].features[0].slices[0]);
            expect(resolution.kind).toBe('event');
            expect(resolution.declaration?.node).toMatchObject({ generation: 2 });
        }
        for (const declarations of ['      event Recorded\n      event Recorded\n', '      event Recorded\n      operation Recorded\n        uses Mailer\n']) {
            const application = parse(prefix + declarations).value;
            expect(new AuthoringProductionResolver(application).resolve('Recorded', application.modules[0].features[0].slices[0]).kind).toBe('ambiguous');
        }
    });
});
