// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { exampleCompletions } from '../example-authoring';
import { completionEntriesFor } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { scanDocument } from '../symbols';
import { responseAnalysis } from '../response-analysis';
import { createTokensProvider } from '../tokens';

const fixture = readFileSync(new URL('../../../Compiler/Conformance/specification-examples.play', import.meta.url), 'utf8').trimEnd().split('\n');
const hover = (lines: string[], text: string, name: string, application = scanDocument(lines)) => {
    const line = lines.findIndex(line => line.trim() === text);
    const column = lines[line].indexOf(name) + 1;
    return hoverContent(lines, line, name, column, column + name.length, application);
};
const entries = (lines: string[], text: string) => {
    const line = lines.findIndex(line => line.trim() === text);
    const before = lines[line].replace(/[\w.]+$/, '');
    const current = lines.map((text, index) => index === line ? before : text);
    return exampleCompletions(current, line, before)?.map(entry => entry.label);
};

describe('when authoring typed examples', () => {
    it('should offer the declaration at every supported container', () => {
        for (const chain of [[], ['module'], ['feature'], ['slice']]) expect(completionEntriesFor(chain).map(entry => entry.label)).toContain('example');
    });
    it('should complete underlying types, but never examples or value types in the header', () => {
        const line = fixture.findIndex(line => line.includes('example AcmeInput'));
        const before = '      example Another : ';
        const current = fixture.map((text, index) => index === line ? before : text);
        const targets = exampleCompletions(current, line, before)!.map(entry => entry.label);
        expect(targets).toEqual(expect.arrayContaining(['RegisterInvoice', 'InvoiceRegistered', 'InvoiceBalance']));
        expect(targets).not.toContain('RootInvoice');
        expect(targets).not.toContain('ReceiptId');
    });
    it.each([
        ['given RootInvoice total = 3000', 'RootInvoice', 'AcmeInput'],
        ['given readmodel ModuleBalance total = 3000', 'ModuleBalance', 'RootInvoice'],
        ['when AcmeInput total = 5000', 'AcmeInput', 'RootInvoice'],
        ['then RootInvoice total = 5000', 'RootInvoice', 'AcmeInput'],
        ['then readmodel ModuleBalance exactly total = 5000', 'ModuleBalance', 'RootInvoice'],
        ['when append RootInvoice total = 6000', 'RootInvoice', 'AcmeInput'],
    ])('should complete the correct kind in %s', (text, included, excluded) => {
        const line = fixture.findIndex(line => line.trim() === text);
        const before = fixture[line].slice(0, fixture[line].indexOf(included));
        const current = fixture.map((text, index) => index === line ? before : text);
        const labels = exampleCompletions(current, line, before)!.map(entry => entry.label);
        expect(labels).toContain(included);
        expect(labels).not.toContain(excluded);
    });
    it('should show merged values, replaced values, generated fixtures and destination origins', () => {
        expect(hover(fixture, 'when AcmeInput total = 5000', 'AcmeInput')).toContain('total = 5000 — override (replaces 2000 from AcmeInput)');
        expect(hover(fixture, 'when AcmeInput total = 5000', 'AcmeInput')).toContain('generated receipt = "22222222-2222-2222-2222-222222222222" — example AcmeInput');
        expect(hover(fixture, 'given RootInvoice total = 3000', 'RootInvoice')).toContain('for "11111111-1111-1111-1111-111111111111" — example RootInvoice');
        expect(hover(fixture, 'when FeatureInput', 'FeatureInput')).toContain('total = 7000 — override');
        expect(hover(fixture, 'example RootInvoice : Invoices.Registration.RegisterInvoice.InvoiceRegistered', 'RootInvoice')).toContain('total = 1000 — example RootInvoice');
    });
    it('should distinguish authored fixture origins from example overrides', () => {
        const plain = fixture.map(line => line.replace('when AcmeInput total = 5000', 'when RegisterInvoice total = 5000'));
        expect(hover(plain, 'when RegisterInvoice total = 5000', 'RegisterInvoice')).toContain('total = 5000 — authored');
    });
    it('should display structured values and overridden destinations without inventing omitted properties', () => {
        const source = ['type Address', '  street String', 'event E', '  address Address', '  amount Int', 'example One : E', '  address = { "street": "first" }', '  for "one"', 'specification S', '  when append One amount = 42', '    for "two"'];
        const content = hover(source, 'when append One amount = 42', 'One');
        expect(content).toContain('address = { "street": "first" } — example One');
        expect(content).toContain('amount = 42 — override');
        expect(content).toContain('for "two" — override (replaces "one" from One)');
    });
    it('should preserve matching and not claim execution', () => {
        const content = hover(fixture, 'then readmodel ModuleBalance exactly total = 5000', 'ModuleBalance');
        expect(content).toContain('Matching is unchanged');
        expect(content).toContain('not execution results');
    });
    it('should resolve qualified names and declaration-scoped types across placed documents', () => {
        const source = ['slice StateChange Check', '  specification S', '    when Other.Input total = 42'];
        const application = { ...scanDocument([]), authoringPath: 'spec.play', authoringPlacement: ['M', 'F'], authoringDocuments: [
            { path: 'input.play', placement: ['M', 'F'], source: 'slice StateChange Other\n  command C\n    total Int\n  example Input : C\n    total = 1' },
            { path: 'wrong.play', placement: ['M', 'Different'], source: 'slice StateChange Other\n  command C\n    text String\n  example Input : C\n    text = "wrong"' },
        ] };
        // Suffix qualification Other.Input is ambiguous across the two modules/features.
        expect(hover(source, source[2].trim(), 'Other.Input', application)).toContain('Ambiguous');
        const qualified = source.map(line => line.replace('Other.Input', 'F.Other.Input'));
        expect(hover(qualified, qualified[2].trim(), 'F.Other.Input', application)).toContain('total = 42 — override (replaces 1 from Input)');
    });
    it('should fail closed on a kind mismatch or duplicate overrides', () => {
        const mismatched = fixture.map(line => line.replace('when AcmeInput total = 5000', 'when RootInvoice total = 5000'));
        expect(hover(mismatched, 'when RootInvoice total = 5000', 'RootInvoice')).toContain('kind mismatch');
        const repeated = [...fixture];
        const line = repeated.findIndex(line => line.includes('when AcmeInput total = 5000'));
        repeated.splice(line + 1, 0, '          total = 6000');
        expect(hover(repeated, 'when AcmeInput total = 5000', 'AcmeInput')).toContain('Invalid duplicate assignments');
    });
    it('should not own comments, quoted names, code fences or an unsupported step', () => {
        const line = fixture.findIndex(line => line.includes('when FeatureInput'));
        const current = fixture.map((text, index) => index === line ? text + ' // FeatureInput' : text);
        const column = current[line].lastIndexOf('FeatureInput') + 1;
        expect(hoverContent(current, line, 'FeatureInput', column, column + 12)).toBeNull();
        expect(exampleCompletions(['description', '  ```text', '  example X : ', '  ```'], 2, '  example X : ')).toBeNull();
        expect(exampleCompletions(fixture, line, '        when query ')).toBeNull();
        expect(exampleCompletions(fixture, line, '        then result ')).toBeNull();
    });
    it('should retain BMP Unicode example names in completion and hover', () => {
        const current = fixture.map(line => line.replaceAll('AcmeInput', 'AcméInput'));
        expect(hover(current, 'when AcméInput total = 5000', 'AcméInput')).toContain('replaces 2000 from AcméInput');
        const line = current.findIndex(line => line.includes('when AcméInput'));
        expect(exampleCompletions(current, line, '        when Acmé')?.map(entry => entry.label)).toContain('AcméInput');
        const rules = createTokensProvider([]).tokenizer.root as unknown as [RegExp, unknown][];
        expect(rules[0][0].test('      example Acmé : Invoice')).toBe(true);
    });
    it('should share the bounded parsed revision with other editor features', () => expect(responseAnalysis(fixture)).toBe(responseAnalysis([...fixture])));
    it('should highlight the example header before generic keyword rules', () => {
        const rules = createTokensProvider([]).tokenizer.root as unknown as [RegExp, unknown][];
        const match = rules[0][0].exec('      example Acme : M.F.C // note');
        expect(match?.slice(1)).toEqual(['      ', 'example', ' ', 'Acme', ' ', ':', ' ', 'M.F.C']);
        expect(rules[0][1]).toEqual(['white', 'keyword', 'white', 'type.identifier', 'white', 'operator', 'white', 'type.identifier']);
        expect(rules[0][0].test('      example String')).toBe(false);
    });
    it('should keep query plans outside the six fixture slots', () => expect(entries(fixture, 'when FeatureInput')).not.toContain('queries'));
});
